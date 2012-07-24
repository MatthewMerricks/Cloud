// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    internal class ParsedMediaTypeHeaderValue
    {
        private const string MediaRangeAsterisk = "*";
        private const char MediaTypeSubTypeDelimiter = '/';
        private const string QualityFactorParameterName = "q";
        private const double DefaultQualityFactor = 1.0;

        private MediaTypeHeaderValue mediaType;
        private string type;
        private string subType;
        private bool? hasNonQualityFactorParameter;
        private double? qualityFactor;

        public ParsedMediaTypeHeaderValue(MediaTypeHeaderValue mediaType)
        {
            Fx.Assert(mediaType != null, "The 'mediaType' parameter should not be null.");

            this.mediaType = mediaType;
            string[] splitMediaType = mediaType.MediaType.Split(MediaTypeSubTypeDelimiter);

            Fx.Assert(splitMediaType.Length == 2, "The constructor of the MediaTypeHeaderValue would have failed if there wasn't a type and subtype.");

            this.type = splitMediaType[0];
            this.subType = splitMediaType[1];
        }

        public string Type
        {
            get
            {
                return this.type;
            }
        }

        public string SubType
        {
            get
            {
                return this.subType;
            }
        }

        public bool IsAllMediaRange
        {
            get
            {
                return this.IsSubTypeMediaRange && string.Equals(MediaRangeAsterisk, this.Type, StringComparison.Ordinal);
            }
        }

        public bool IsSubTypeMediaRange
        {
            get
            {
                return string.Equals(MediaRangeAsterisk, this.SubType, StringComparison.Ordinal);
            }
        }

        public bool HasNonQualityFactorParameter
        {
            get
            {
                if (!this.hasNonQualityFactorParameter.HasValue)
                {
                    this.hasNonQualityFactorParameter = false;
                    foreach (NameValueHeaderValue param in this.mediaType.Parameters)
                    {
                        if (!string.Equals(QualityFactorParameterName, param.Name, StringComparison.Ordinal))
                        {
                            this.hasNonQualityFactorParameter = true;
                        }
                    }
                }

                return this.hasNonQualityFactorParameter.Value;
            }
        }

        public string CharSet
        {
            get
            {
                return this.mediaType.CharSet;
            }
        }

        public double QualityFactor
        {
            get
            {
                if (!this.qualityFactor.HasValue)
                {
                    MediaTypeWithQualityHeaderValue mediaTypeWithQuality = this.mediaType as MediaTypeWithQualityHeaderValue;
                    if (mediaTypeWithQuality != null)
                    {
                        this.qualityFactor = mediaTypeWithQuality.Quality;
                    }

                    if (!this.qualityFactor.HasValue)
                    {
                        this.qualityFactor = DefaultQualityFactor;
                    }
                }

                return this.qualityFactor.Value;
            }
        }      
    }
}