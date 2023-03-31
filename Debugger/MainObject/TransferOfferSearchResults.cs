using System.Linq;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace ModTools.MainObject
{
    public class TransferOfferSearchResults : Singleton<TransferOfferSearchResults>
    {
        private static GameObject thisGameObject;

        private InstanceID m_instanceID;
        private TransferManager.TransferOffer[] m_outgoingOffers;
        private TransferManager.TransferOffer[] m_incomingOffers;

        public TransferManager.TransferOffer[] IncomingOffers() => m_incomingOffers;

        public TransferManager.TransferOffer[] OutgoingOffers() => m_outgoingOffers;

        public InstanceID InstanceID => m_instanceID;

        public static void Bootstrap()
        {
            if (thisGameObject != null)
            {
                return;
            }

            thisGameObject = new GameObject("TransferOfferSearchResults")
            {
                transform =
                {
                    parent = GameObject.Find(ModToolsMod.ModToolsName).transform,
                },
            };
            thisGameObject.AddComponent<TransferOfferSearchResults>();
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

        public void Find(InstanceID instanceID)
        {
            var outgoingOffers =
                (TransferManager.TransferOffer[])typeof(TransferManager).GetField("m_outgoingOffers",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(TransferManager.instance);
            var incomingOffers =
                (TransferManager.TransferOffer[])typeof(TransferManager).GetField("m_incomingOffers",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(TransferManager.instance);
            m_instanceID = instanceID;
            m_outgoingOffers = outgoingOffers
                .Where(offer => offer.Amount > 0 && offer.m_object == instanceID).ToArray();
            m_incomingOffers = incomingOffers
                .Where(offer => offer.Amount > 0 && offer.m_object == instanceID).ToArray();
        }
    }
}