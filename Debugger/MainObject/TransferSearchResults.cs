using ColossalFramework;
using UnityEngine;

namespace ModTools.MainObject
{
    public class TransferSearchResults : Singleton<TransferSearchResults>
    {
        private static GameObject thisGameObject;

        private TransferManager.TransferOffer[] m_outgoingOffers;
        private TransferManager.TransferOffer[] m_incomingOffers;

        public TransferManager.TransferOffer[] IncomingOffers() => m_incomingOffers;

        public TransferManager.TransferOffer[] OutgoingOffers() => m_outgoingOffers;

        public static void Bootstrap()
        {
            if (thisGameObject != null)
            {
                return;
            }

            thisGameObject = new GameObject("Custom Prefabs")
            {
                transform =
                {
                    parent = GameObject.Find(ModToolsMod.ModToolsName).transform,
                },
            };
            thisGameObject.AddComponent<CustomPrefabs>();
        }

        public static void Revert()
        {
            if (thisGameObject == null)
            {
                return;
            }

            Destroy(thisGameObject);
            thisGameObject = null;
        }
    }
}