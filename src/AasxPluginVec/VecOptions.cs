/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPluginVec
{
    public class VecOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        public string TemplateIdAsset = "www.example.com/ids/asset/DDDD_DDDD_DDDD_DDDD";
        public string TemplateIdAas = "www.example.com/ids/aas/DDDD_DDDD_DDDD_DDDD";
        public string TemplateIdSubmodel = "www.example.com/ids/submodel/DDDD_DDDD_DDDD_DDDD";
        public Dictionary<string, string> AssetIdByPartNumberDict = new Dictionary<string, string>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static VecOptions CreateDefault()
        {
            var opt = new VecOptions();
            return opt;
        }
    }
}