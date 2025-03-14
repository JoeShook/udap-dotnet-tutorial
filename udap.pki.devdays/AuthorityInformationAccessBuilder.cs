﻿#region (c) 2022 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace udap.pki.devdays
{
    public sealed class AuthorityInformationAccessBuilder
    {
        private List<byte[]> _encodedUrls = new List<byte[]>();
        private readonly List<byte[]> _encodedSequences = new List<byte[]>();


        /// <summary>
        /// Adding ObjectIdentifier (OID) 1.3.6.1.5.5.7.48.2 for a list of URIs
        /// </summary>
        /// <param name="uris"></param>
        public void AddCertificateAuthorityIssuerUris(List<Uri> uris)
        {
            if (uris == null || uris.Count == 0)
                throw new ArgumentException("At least one URI must be provided.", nameof(uris));

            foreach (var uri in uris)
            {
                if (uri == null)
                    throw new ArgumentException("URIs cannot be null.", nameof(uris));

                AddUri(uri);
            }
        }

        private void AddUri(Uri uri)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.2");
            var encodedOid = writer.Encode();

            writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(
                UniversalTagNumber.IA5String,
                uri.AbsoluteUri,
                new Asn1Tag(TagClass.ContextSpecific, 6));
            var encodedUri = writer.Encode();

            writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(encodedOid);
                writer.WriteEncodedValue(encodedUri);
            }

            _encodedSequences.Add(writer.Encode());
        }

        public X509Extension Build(bool critical = false)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach (byte[] encodedName in _encodedSequences)
                {
                    writer.WriteEncodedValue(encodedName);
                }
            }
            return new X509Extension(
                // Oids.SubjectAltName,
                "1.3.6.1.5.5.7.1.1",
                writer.Encode(),
                critical);
        }
    }
}
