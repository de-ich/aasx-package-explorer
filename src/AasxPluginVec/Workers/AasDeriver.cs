/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AasxIntegrationBase;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVec.BasicAasUtils;

namespace AasxPluginVec
{
    /// <summary>
    /// This class allows to derive a new AAS from an existing on which is then linked via the specific asset ID.
    /// </summary>
    public class AasDeriver
    {
        public AasDeriver(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            VecOptions options)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));   
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected VecOptions options;

        // things specified in 'CreateOrder(...)
        protected string nameOfDerivedAas;

        // the aas to be created/derived
        protected AssetAdministrationShell derivedAas;

        public IAssetAdministrationShell DeriveAas(
            string nameOfDerivedAas)
        {
            this.nameOfDerivedAas = nameOfDerivedAas ?? throw new ArgumentNullException(nameof(nameOfDerivedAas));

            DoDeriveAas();

            return derivedAas;
        }

        private void DoDeriveAas()
        {
            // create the new (derived) aas
            derivedAas = CreateAAS(nameOfDerivedAas, options.TemplateIdAas, options.TemplateIdAsset, env);

            // copy the specific asset ID(s) of the orignal AAS to establish a link to this AAS
            derivedAas.AssetInformation.SpecificAssetIds = aas.AssetInformation.SpecificAssetIds?.Copy();

            // copy (references to) all submodels in the original AAS
            derivedAas.Submodels = aas.Submodels?.Copy();
        }
    }
}
