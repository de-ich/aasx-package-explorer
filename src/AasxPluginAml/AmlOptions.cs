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

namespace AasxPluginAml;

public class AmlOptions : AasxIntegrationBase.AasxPluginOptionsBase
{
    /// <summary>
    /// Create a set of minimal options
    /// </summary>
    public static AmlOptions CreateDefault()
    {
        var opt = new AmlOptions();
        return opt;
    }
}
