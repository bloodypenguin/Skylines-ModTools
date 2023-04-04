using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace ModTools.MainObject
{
    public partial class TransferOfferSearchResults : Singleton<TransferOfferSearchResults>
    {
        private static GameObject thisGameObject;

        private InstanceID m_instanceID;
        private TransferOfferSearchResult[] m_outgoingOffers;
        private TransferOfferSearchResult[] m_incomingOffers;

        public TransferOfferSearchResult[] IncomingOffers() => m_incomingOffers;

        public TransferOfferSearchResult[] OutgoingOffers() => m_outgoingOffers;

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
            var outgoingCount =
                (ushort[])typeof(TransferManager).GetField("m_outgoingCount",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(TransferManager.instance);
            var incomingCount =
                (ushort[])typeof(TransferManager).GetField("m_incomingCount",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(TransferManager.instance);
            var incomingAmount =
                (int[])typeof(TransferManager).GetField("m_incomingAmount",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(TransferManager.instance);

            m_instanceID = instanceID;
            var outgoingResults = new List<TransferOfferSearchResult>();
            var incomingResults = new List<TransferOfferSearchResult>();
            for (var material = 0; material < incomingAmount.Length; material++)
            {
                var materialOffset = material * 8;

                for (var priority = 7; priority >= 0; --priority)
                {
                    var offerOffset = materialOffset + priority;
                    for (var offerIndex = 0; offerIndex < outgoingCount[offerOffset]; offerIndex++)
                    {
                        var valueIndex = (uint)((offerOffset * 256) + offerIndex);
                        var offer = outgoingOffers[valueIndex];
                        if (offer.m_object == instanceID)
                        {
                            outgoingResults.Add(new TransferOfferSearchResult(valueIndex, offer));
                        }
                    }
                }

                for (var priority = 7; priority >= 0; --priority)
                {
                    var offerOffset = materialOffset + priority;
                    for (var offerIndex = 0; offerIndex < incomingCount[offerOffset]; offerIndex++)
                    {
                        var valueIndex = (uint)((offerOffset * 256) + offerIndex);
                        var offer = incomingOffers[valueIndex];
                        if (offer.m_object == instanceID)
                        {
                            incomingResults.Add(new TransferOfferSearchResult(valueIndex, offer));
                        }
                    }
                }
            }

            m_incomingOffers = incomingResults.ToArray();
            m_outgoingOffers = outgoingResults.ToArray();

        }

        public static TransferManager.TransferReason OfferMaterial(uint valueIndex)
        {
            return (TransferManager.TransferReason)(valueIndex / 256 / 8);
        }
    }
}