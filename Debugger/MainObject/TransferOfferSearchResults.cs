using System.Collections.Generic;
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
        private SearchResult[] m_outgoingOffers;
        private SearchResult[] m_incomingOffers;

        public SearchResult[] IncomingOffers() => m_incomingOffers;

        public SearchResult[] OutgoingOffers() => m_outgoingOffers;

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
            var list = new List<SearchResult>();
            for (uint index = 0; index < outgoingOffers.Length; index++)
            {
                var offer = outgoingOffers[index];
                if (offer.m_object == instanceID) list.Add(new SearchResult(index, offer));
            }

            m_outgoingOffers = list.ToArray();
            var list1 = new List<SearchResult>();
            for (uint index = 0; index < incomingOffers.Length; index++)
            {
                var offer = incomingOffers[index];
                if (offer.m_object == instanceID) list1.Add(new SearchResult(index, offer));
            }

            m_incomingOffers = list1.ToArray();
        }

        public struct SearchResult
        {
            private TransferManager.TransferOffer transferOffer;
            private uint valueIndex;

            public SearchResult(uint valueIndex, TransferManager.TransferOffer transferOffer)
            {
                this.valueIndex = valueIndex;
                this.transferOffer = transferOffer;
            }

            public TransferManager.TransferOffer TransferOffer => transferOffer;

            public uint ValueIndex => valueIndex;
        }
    }
}