using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;
using static AasxPluginVec.BomSMUtils;
using System.Xml.Linq;

namespace AasxPluginVec
{
    public class BasicAasUtils
    {
        private static Random MyRnd = new Random();
        private static string lastGeneratedId = null;

        // The version of 'GenerateIdAccordingTemplate' from 'AdminShellUtil' does not ensure unique IDs when
        // being called multiple times in rapid succession (more than two times in one ten thousandths of a second).
        // Hence, we dupliate and adapt this method to use random numbers instead of times as base for id generation.
        public static string GenerateIdAccordingTemplate(string tpl)
        {
             // generate a deterministic decimal digit string
             var decimals = String.Format("{0:fffffffyyMMddHHmmss}", DateTime.UtcNow);
             decimals = new string(decimals.Reverse().ToArray());
             // convert this to an int
             if (!Int64.TryParse(decimals, out Int64 decii))
                decii = MyRnd.Next(Int32.MaxValue);
            
            // make an hex out of this
            string hexamals = decii.ToString("X");
            // make an alphanumeric string out of this
            string alphamals = "";
            var dii = decii;
            while (dii >= 1)
            {
                var m = dii % 26;
                alphamals += Convert.ToChar(65 + m);
                dii = dii / 26;
            }

            // now, "salt" the strings
            for (int i = 0; i < 32; i++)
            {
                var c = Convert.ToChar(48 + MyRnd.Next(10));
                decimals += c;
                hexamals += c;
                alphamals += c;
            }

            // now, can just use the template
            var id = "";
            foreach (var tpli in tpl)
            {
                if (tpli == 'D' && decimals.Length > 0)
                {
                    id += decimals[0];
                    decimals = decimals.Remove(0, 1);
                }
                else
                if (tpli == 'X' && hexamals.Length > 0)
                {
                    id += hexamals[0];
                    hexamals = hexamals.Remove(0, 1);
                }
                else
                if (tpli == 'A' && alphamals.Length > 0)
                {
                    id += alphamals[0];
                    alphamals = alphamals.Remove(0, 1);
                }
                else
                    id += tpli;
            }

            // added code
            if (id == lastGeneratedId)
            {
                var lastChar = id.ToArray().Last();
                int last = 0;
                Int32.TryParse(lastChar.ToString(), out last);
                last = (last + 1) % 10; // change the existing id
                id = id.Substring(0, id.Length - 1) + last;
            }
            lastGeneratedId = id;

            // ok
            return id;
        }

        /**
         * An adapted version of 'GetReference' from 'AdminShell.cs'. This version does not include
         * the id of the containing AAS in the reference keys because this leads to errors with
         * the 'jump' function in the package explorer
         */
        public static Reference GetReference(SubmodelElement element, bool includeParents = true)
        {
            Reference r = new Reference();
            // this is the tail of our referencing chain ..
            r.Keys.Add(Key.CreateNew(element.GetElementName(), true, "IdShort", element.idShort));
            // try to climb up ..
            var current = element.parent;
            while (includeParents && current != null)
            {
                if (current is Identifiable cid)
                {
                    // add big information set
                    r.Keys.Insert(0, Key.CreateNew(
                        current.GetElementName(),
                        true,
                        cid.identification.idType,
                        cid.identification.id));
                    break; // changed from the official version
                }
                else
                if (current is Referable crf)
                {
                    // reference via idShort
                    r.Keys.Insert(0, Key.CreateNew(
                        current.GetElementName(),
                        true,
                        "IdShort", crf.idShort));
                }

                if (current is Referable crf2)
                    current = crf2.parent;
                else
                    current = null;
            }
            return r;
        }
    }
}
