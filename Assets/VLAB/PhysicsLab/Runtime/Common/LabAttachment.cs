using System;
using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    public sealed class LabAttachment : MonoBehaviour
    {
        [SerializeField] private string attachmentType = "generic";
        public string AttachmentType => attachmentType;

        public void Configure(string type)
        {
            attachmentType = string.IsNullOrWhiteSpace(type) ? "generic" : type;
        }
    }
}
