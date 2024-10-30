using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

namespace MoreCounterplay.Behaviours
{
	/// <summary>
	/// 	Network behaviour script handling placing items on top of a Jester.
	/// </summary>
	public class JesterSurface : PlaceableObjectsSurface
	{
		/// <summary>
		/// 	Prefab for the Jester surface container.
		/// </summary>
		public static GameObject? JesterSurfacePrefab { get; internal set; }

		/// <summary>
		/// 	Total weight of items on top of the Jester.
		/// </summary>
		/// <remarks>
		/// 	Might not be 100% accurate with the weight of items shown in-game, due to rounding.
		/// 	Weight from items that are destroyed or removed by other means will also remain until it resets to 0.
		/// </remarks>
		public float TotalWeight { get; internal set; } = 0f;

		/// <summary>
		/// 	Jester script instance the surface is parented to.
		/// </summary>
		public JesterAI? Jester { get; private set; }

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			// Remove the '(Clone)' from the Jester surface instance.
			transform.name = JesterSurfacePrefab?.name;

			// Assign PlaceableObjectsSurface components.
			parentTo = GetComponent<NetworkObject>();
			placeableBounds = transform.Find("bounds").GetComponent<Collider>();
			triggerScript = GetComponent<InteractTrigger>();

			// Call 'JesterSurface.PlaceObject()' whenever the player finishes interacting with the Jester surface.
			triggerScript.onInteract.AddListener(PlaceObject);
		}

		public void Start()
		{
			// Find and obtain parent Jester script.
			if (transform.GetParent()?.TryGetComponent(out JesterAI jester) == true)
			{
				Jester = jester;
			}

			// Set Jester surface layer to InteractableObject (where it's supposed to be).
			gameObject.layer = LayerMask.NameToLayer("InteractableObject");

			// Add Jester box as source and activate constraint to move alongside it.
			ParentConstraint jesterConstraint = gameObject.AddComponent<ParentConstraint>();
			jesterConstraint.AddSource(new()
			{
				weight = 1f,
				sourceTransform = transform.GetParent().Find("MeshContainer/AnimContainer/metarig/BoxContainer")
			});
			jesterConstraint.constraintActive = true;
		}

		public new void Update()
		{
			if (triggerScript != null)
			{
				// Disable interacting with Jester surface if the local player is not holding an item, or if the Jester is active.
				triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.isHoldingObject && Jester?.currentBehaviourStateIndex != 2;
			}
		}

		/// <summary>
		/// 	Place the currently held object on top of the Jester surface.
		/// </summary>
		/// <param name="playerWhoTriggered">The player placing the item.</param>
		public new void PlaceObject(PlayerControllerB playerWhoTriggered)
		{
			if (!playerWhoTriggered.IsOwner) return;

			// Return if the local player is not holding an item.
			if (!playerWhoTriggered.isHoldingObject || playerWhoTriggered.isGrabbingObjectAnimation || playerWhoTriggered.currentlyHeldObjectServer == null) return;

			// Obtain item to place on top of the Jester.
			GrabbableObject item = playerWhoTriggered.currentlyHeldObjectServer;

			// Obtain placement position and place object on top of the Jester.
			Vector3 position = parentTo.transform.InverseTransformPoint(itemPlacementPosition(playerWhoTriggered.gameplayCamera.transform, item));
			playerWhoTriggered.DiscardHeldObject(true, parentTo, position, false);

			// Place item on top of the Jester on all clients.
			PlaceItemOnClient(item);
			PlaceItemServerRpc(playerWhoTriggered.GetComponent<NetworkObject>(), item.GetComponent<NetworkObject>());
		}

		/// <summary>
		/// 	Obtain the closest point within the surface bounds that the camera is pointing to.
		/// </summary>
		/// <param name="camera">The local player camera.</param>
		/// <param name="heldItem">The local player's currently held item.</param>
		/// <returns>The Vector3 position at which to place the item.</returns>
		public new Vector3 itemPlacementPosition(Transform camera, GrabbableObject heldItem)
		{
			// Layers: [8 (Room), 9 (InteractableObject), 11 (Colliders), 30 (Vehicle)] + 13 (Triggers) + 19 (Enemies).
			int layerMask = 1073744640 + (1 << 13) + (1 << 19);

			// Find and return closest point within the surface bounds that the player camera is aiming towards.
			bool raycastHit = Physics.Raycast(camera.position, camera.forward, out RaycastHit hitInfo, 7f, layerMask, QueryTriggerInteraction.Ignore);
			return !raycastHit ? placeableBounds.transform.position : (placeableBounds.ClosestPoint(hitInfo.point)
				+ placeableBounds.transform.up * heldItem.itemProperties.verticalOffset);
		}

		/// <summary>
		/// 	Reset the parent Jester's animations under certain circumstances.
		/// </summary>
		public void ResetJester()
		{
			if (Jester == null) return;

			// Disable Jester encumbered state if its weight is past the 'encumber' threshold.
			if (MoreCounterplay.Settings.JesterEncumberThreshold != 0 && TotalWeight < MoreCounterplay.Settings.JesterEncumberThreshold
				&& Jester.stunNormalizedTimer > 0f)
			{
				Jester.beginCrankingTimer = Jester.stunNormalizedTimer * Jester.enemyType.stunTimeMultiplier;
				Jester.stunNormalizedTimer = 0f;
			}

			// Disable Jester panicked state if its weight is past the 'panic' threshold.
			if (MoreCounterplay.Settings.JesterPanicThreshold != 0 && TotalWeight < MoreCounterplay.Settings.JesterPanicThreshold
				&& Jester.creatureAnimator.GetBool("turningCrank") && Jester.currentBehaviourStateIndex == 0)
			{
				Jester.creatureVoice.Stop();

				Jester.creatureAnimator.SetBool("turningCrank", false);
				Jester.creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);

				Jester.creatureAnimator.CrossFade("IdleDocile", 0.1f);
			}
		}

		/// <summary>
		/// 	Place an item on top of the Jester on the local client.
		/// </summary>
		/// <param name="item">The item to place.</param>
		public void PlaceItemOnClient(GrabbableObject item)
		{
			// Return if any PlaceableObjectsSurface component is missing.
			if (parentTo == null || placeableBounds == null || triggerScript == null) return;

			// Shouldn't be able to interact with the surface at all while the Jester is active, but checking here regardless.
			if (Jester == null || Jester.currentBehaviourStateIndex == 2) return;

			// Add a Rigidbody component to the item's scan node (if it doesn't already have one) in order for it to remain scannable while on top of the Jester.
			if (item.transform.GetChild(0)?.TryGetComponent(out ScanNodeProperties itemScanNode) == true
				&& !itemScanNode.TryGetComponent<Rigidbody>(out _))
			{
				// Fix obtained from: https://github.com/digger1213/CruiserImproved/blob/6159533fee9c5748ef667067a9cbb66ba5fa6237/source/Patches/PlayerController.cs#L45
				itemScanNode.gameObject.AddComponent<Rigidbody>().isKinematic = true;
			}

			// Increment Jester's total weight by the weight of the placed item.
			TotalWeight += (item.itemProperties.weight - 1f) * 105f;
			MoreCounterplay.Log($"Current weight: {TotalWeight}");

			// Update Jester scan node to include its total weight.
			if (MoreCounterplay.Settings.ShowWeightOnScan && Jester.transform.Find("ScanNode")?.TryGetComponent(out ScanNodeProperties jesterScanNode) == true)
			{
				jesterScanNode.subText = $"Weight: {TotalWeight} lbs";
			}

			// Set Jester as stunned if its weight goes past the 'encumber' threshold, to prevent it from moving around.
			if (MoreCounterplay.Settings.JesterEncumberThreshold > 0 && TotalWeight >= MoreCounterplay.Settings.JesterEncumberThreshold
				&& Jester.stunNormalizedTimer <= 0f)
			{
				if (!Jester.creatureAnimator.GetBool("turningCrank"))
				{
					// Stun Jester for the same amount of time as the timer until cranking.
					Jester.stunNormalizedTimer = Jester.beginCrankingTimer / Jester.enemyType.stunTimeMultiplier;
					Jester.beginCrankingTimer *= 15f; // Timer until cranking decreases 15x as fast when the Jester is stunned.
				}
			}

			// Enable Jester panicked state if its weight goes past the 'panic' threshold.
			if (MoreCounterplay.Settings.JesterPanicThreshold > 0 && TotalWeight >= MoreCounterplay.Settings.JesterPanicThreshold)
			{
				// Enable Jester panicked state (if not already cranking).
				if (!Jester.creatureAnimator.GetBool("turningCrank"))
				{
					// Play Jester ambient noise as a warning.
					Jester.creatureVoice.clip = Jester.screamingSFX;
					Jester.creatureVoice.Play();

					// Enable frantic Jester cranking.
					Jester.creatureAnimator.SetBool("turningCrank", true);
					Jester.creatureAnimator.SetFloat("CrankSpeedMultiplier", 2.5f);

					// Set timer until cranking to 5 seconds (at most), at which point it'll pop instantly if its weight is still higher than the 'panic' threshold.
					Jester.beginCrankingTimer = Mathf.Clamp(Jester.beginCrankingTimer, MoreCounterplay.Settings.MinPanicTimer * 15f, MoreCounterplay.Settings.MaxPanicTimer * 15f);
					Jester.stunNormalizedTimer = Jester.beginCrankingTimer / (Jester.enemyType.stunTimeMultiplier * 15f);
				}
				else
				{
					// Activate Jester immediately if an item is placed on it while cranking AND past the 'panic' threshold.
					Jester.SwitchToBehaviourStateOnLocalClient(2);
					Jester.stunNormalizedTimer = 0f;

					DropAllItemsOnClient();
				}
			}
		}

		/// <summary>
		/// 	Grab an item placed on top of the Jester on the local client.
		/// </summary>
		/// <param name="item">The item to grab.</param>
		public void RemoveItemOnClient(GrabbableObject item)
		{
			// Return if any PlaceableObjectsSurface component is missing.
			if (parentTo == null || placeableBounds == null || triggerScript == null) return;

			// Shouldn't be able to interact with the surface at all while the Jester is active, but checking here regardless.
			if (Jester == null || Jester.currentBehaviourStateIndex == 2) return;

			TotalWeight -= (item.itemProperties.weight - 1f) * 105f;
			MoreCounterplay.Log($"Current weight: {TotalWeight}");

			// De-parent item from Jester surface.
			item.transform.SetParent(StartOfRound.Instance.propsContainer, true);

			if (MoreCounterplay.Settings.ShowWeightOnScan && Jester.transform.Find("ScanNode").TryGetComponent(out ScanNodeProperties scanNode) == true)
			{
				scanNode.subText = TotalWeight > 0 ? $"Weight: {TotalWeight} lbs" : "";
			}

			if (Jester.currentBehaviourStateIndex == 0)
			{
				ResetJester();
			}
		}

		/// <summary>
		/// 	Drop all items on top of the Jester on the local client.
		/// </summary>
		/// <param name="hit">Whether or not the Jester was hit instead of popping by itself (to reset animations).</param>
		public void DropAllItemsOnClient(bool hit = false)
		{
			// Iterate for every item on top of the Jester.
			GetComponentsInChildren<GrabbableObject>().Do(item =>
			{
				// De-parent item from Jester surface.
				item.parentObject = null;
				item.transform.SetParent(StartOfRound.Instance.propsContainer, true);

				// Set placed item properties.
				item.EnablePhysics(true);
				item.EnableItemMeshes(true);
				item.isHeld = false;
				item.isPocketed = false;

				// Set falling properties and start its fall.
				item.startFallingPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(item.transform.position);
				item.FallToGround(false);
				item.fallTime = -0.3f;
				item.hasHitGround = false;
			});

			// Clear Jester scan node's subtext, and reset its total weight.
			if (Jester?.transform.Find("ScanNode")?.TryGetComponent(out ScanNodeProperties scanNode) == true)
			{
				scanNode.subText = "";
			}
			TotalWeight = 0.0f;

			// Reset Jester animations if hit.
			if (hit)
			{
				ResetJester();
			}
		}

		/// <summary>
		/// 	Handles switching a Jester's animation both when its pop is prevented and when it finishes panicking.
		/// </summary>
		/// <param name="panic">Whether or not the Jester is in its panicked state.</param>
		[ClientRpc]
		public void SwitchAnimationClientRpc(bool panic = false)
		{
			if (Jester == null) return;

			if (panic || !MoreCounterplay.Settings.ItemsStayOnLid)
			{
				// Show Jester's head for a brief moment before dropping items (cannot actually kill players unless it's panicking).
				Jester.creatureAnimator.CrossFade("JesterPopUp", 0.1f);
				Jester.creatureVoice.Stop();

				// Drop all items on top of the Jester.
				DropAllItemsOnClient();
			}
			else
			{
				// Transition from "JesterTurnCrank" state to "IdleDocile" state in 0.1 seconds.
				Jester.creatureAnimator.CrossFade("IdleDocile", 0.1f);
			}
		}

		/// <summary>
		/// 	Place an item on top the Jester for all clients except the one who placed the item.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the player who placed the item.</param>
		/// <param name="itemReference">NetworkObject reference of the item to place.</param>
		[ClientRpc]
		public void PlaceItemClientRpc(NetworkObjectReference playerReference, NetworkObjectReference itemReference)
		{
			if (playerReference.TryGet(out NetworkObject playerNetworkObject) && playerNetworkObject.IsOwner) return;

			if (itemReference.TryGet(out NetworkObject itemNetworkObject)
				&& itemNetworkObject.TryGetComponent(out GrabbableObject item))
			{
				PlaceItemOnClient(item);
			}
		}

		/// <summary>
		/// 	Send 'JesterSurface.PlaceItemClientRpc()' call from the server to all clients.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the player who placed the item.</param>
		/// <param name="itemReference">NetworkObject reference of the item to place.</param>
		[ServerRpc(RequireOwnership = false)]
		public void PlaceItemServerRpc(NetworkObjectReference playerReference, NetworkObjectReference itemReference)
		{
			PlaceItemClientRpc(playerReference, itemReference);
		}


		/// <summary>
		/// 	Remove an item placed on top of the Jester for all clients except the one who grabbed the item.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the player who grabbed the item</param>
		/// <param name="itemReference">NetworkObject reference of the item to grab.</param>
		[ClientRpc]
		public void RemoveItemClientRpc(NetworkObjectReference playerReference, NetworkObjectReference itemReference)
		{
			if (playerReference.TryGet(out NetworkObject playerNetworkObject) && playerNetworkObject.IsOwner) return;

			if (itemReference.TryGet(out NetworkObject itemNetworkObject)
				&& itemNetworkObject.TryGetComponent(out GrabbableObject item))
			{
				RemoveItemOnClient(item);
			}
		}

		/// <summary>
		/// 	Send 'JesterSurface.RemoveItemClientRpc()' call from the server to all clients.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the player who placed the item.</param>
		/// <param name="itemReference">NetworkObject reference of the item to place.</param>
		[ServerRpc(RequireOwnership = false)]
		public void RemoveItemServerRpc(NetworkObjectReference playerReference, NetworkObjectReference itemReference)
		{
			RemoveItemClientRpc(playerReference, itemReference);
		}

		/// <summary>
		/// 	Drop all items on top of the Jester for all clients except the host, or all clients except the one who hit it.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the host, or the player who hit the Jester.</param>
		/// <param name="hit">Whether or not the Jester was hit instead of popping by itself (to reset animations).</param>
		[ClientRpc]
		public void DropAllItemsClientRpc(NetworkObjectReference playerReference, bool hit = false)
		{
			if (playerReference.TryGet(out NetworkObject playerNetworkObject) && playerNetworkObject.IsOwner) return;

			DropAllItemsOnClient(hit);
		}

		/// <summary>
		/// 	Send 'JesterSurface.DropAllItemsClientRpc()' call from the server to all clients.
		/// </summary>
		/// <param name="playerReference">NetworkObject reference of the host, or the player who hit the Jester.</param>
		/// <param name="hit">Whether or not the Jester was hit instead of popping by itself (to reset animations).</param>
		[ServerRpc(RequireOwnership = false)]
		public void DropAllItemsServerRpc(NetworkObjectReference playerReference, bool hit = false)
		{
			DropAllItemsClientRpc(playerReference, hit);
		}
	}
}