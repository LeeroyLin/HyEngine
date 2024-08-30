using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Engine.Scripts.Runtime.Global
{
    public class GlobalConfigUtil
    {
        /// <summary>
        /// 全局配置路径
        /// </summary>
        private static readonly string GLOBAL_CONFIG_STREAMING_ASSETS_PATH = "Config/GlobalConfig.json";

        public static GlobalConfig Conf;

        private static readonly string GLOBAL_CONFIG_KEY = "8f(i.084@x-]gf.uy3v^.#=#t$22!!73-jtv,n#)$0#a1&y_k$[!wi8tu22sf?&(3!0%i69!u5$ic+r-2j#&i04kdth1__+.n(^3n(x,x2*e*+%6n?&3km2!c42$y?]ff5ct-ew@qh^=i].$thybn&=c8f,0yroo&4$?w$y4swd+%8u*l#9-5j16sz_#z.p4g]6+]m_7tssth$*9?3l4(oyk)1d,p^l5p+3+k%trx=-cy7a?_d[%+q3r-3fmuhsb5(ry684u(](d$azl4t&5maecn][o[h%(r)$*!z]ve+#ae26!hpaig54pz@=,6xkuivhj)+u[a&)yo1zsg+r%c^+2!*fb7.i+,u6rt28uz9-6_&m%q[7h_3](r_zc$dd+dygbf$$e+9=p)q=u&6hd!m9.30on0!d&(qiq+p0di._?%[,ea?n4e6*6]dq0gf4a5[+j9i)4g%+e,7*3vm[9#q#[*b$#%5g7%(h&-cnlm^ty_h4=?3#4[#af5z)]f@ah^y!1.-sf#$1o[6d?*@0p0orpm0gy[fuk_q(5s$a&hk&b5#-x&r.(yq=@dm#,1*+0?u((uzgw72m*_*n).0$)2nhn3)t@1@a@]-9(wdw@_&2a6srkb@hf$fl@9(7=&zr5csyeb8y$arr.ww45t.2,6=ns3gt4=u3zf^_uo2?z0it+iu1pcp_bjh8e7[.#j[qp7[?qsf89hx5pdfrd,^nw0ut&sxg93nnz@km=o[og[&u1^&[@h*(.v2]qep@n34bb71_4f_!ryrphralh!ba!g_1[c)xbqcl3vc3.r6@3u^7*m)kq1np^*p0#h6xznh,o4##96cj&%fh0zb(i6?=*1$d(us[8i(%#td$,l[#k5^r0]!d3o@o8%=mt[-!*@bmkiua^$1l9_?7b7#f!lbv-k,i5i0x!awskz+g=5?0x#8ri29d5o)d]_%f.dyz*(,4k_[=0=ye$2-lunt*aj3l@-),kyl*,^0ciy@?@)i0)?4t(?j9[zc9ema,9,!.khq7e43$wdq(nta8_%t)[c2hf95hxkdj.az,a%@[o?31^?h^v-,!@fc^)(bu#^+3kosu039i1h_[-nq(^=7qe4-z7?$3ay7t,.s*=es?2bhs4zz_&_+d&n5la3s(3h&a%k_(q!$c@h9+m,_xaplny9l-2g#=fhqd7s!6h@z@f0%1]cr&z]_](hv70*-]e1w+l*b!l+[m%qb+!gdcs1o9#y0f@#@r-&]q&67gx.w*].@=5ji=^fj[b?r1h.fwz#4hk6ix.hq!.376#m0^4vf";

        public static async Task LoadConf()
        {
            Conf = await LoadANewConfRuntime();
        }

        public static async Task<GlobalConfig> LoadANewConfRuntime()
        {
            var content = await ReadTextRuntime.ReadSteamingAssetsText(GLOBAL_CONFIG_STREAMING_ASSETS_PATH);
            
            var bytes = Encoding.UTF8.GetBytes(content);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ GLOBAL_CONFIG_KEY[i % GLOBAL_CONFIG_KEY.Length]);

            var str = Encoding.UTF8.GetString(bytes);
            
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(str);
            return conf;
        }

        public static GlobalConfig LoadANewConfEditor()
        {
#if UNITY_EDITOR
            var path = $"{Application.streamingAssetsPath}/{GLOBAL_CONFIG_STREAMING_ASSETS_PATH}";
            var content = File.ReadAllText(path);
            var conf = JsonConvert.DeserializeObject<GlobalConfig>(content);
            return conf;
#endif

            return null;
        }

        public static bool SaveConf(GlobalConfig conf)
        {
#if UNITY_EDITOR
            var savePath = $"{Application.streamingAssetsPath}/{GLOBAL_CONFIG_STREAMING_ASSETS_PATH}";
            var content = JsonConvert.SerializeObject(conf);

            PathUtil.MakeSureDir(Path.GetDirectoryName(savePath));
            
            var bytes = Encoding.UTF8.GetBytes(content);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ GLOBAL_CONFIG_KEY[i % GLOBAL_CONFIG_KEY.Length]);
            
            try
            {
                File.WriteAllBytes(savePath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save global config to {savePath} failed. err: {e.Message}");
                
                return false;
            }
#endif
            
            Debug.Log($"Save global config finished.");
            
            return true;
        }
    }
}