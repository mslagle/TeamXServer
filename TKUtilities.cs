using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TKServerConsole
{

    /*
    public class TKSaveFile
    {
        public int floor;
        public int skybox;
        public List<TKBlock> blocks;
    }

    public class TKBlock
    {
        public Vector3 position { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public List<float> properties { get; set; }
        public int blockID { get; set; }
        public string UID { get; set; }
    }*/

    
    /*
    public static class TKUtilities
    {
        public static TKBlock JSONToTKBlock(string json)
        {
            TKBlock block = JsonConvert.DeserializeObject<TKBlock>(json);
            return block;
        }

        public static string GetJSONString(TKBlock tkBlock)
        {
            return JsonConvert.SerializeObject(tkBlock);
        }

        public static List<float> PropertyStringToList(string properties)
        {
            return properties.Split('|').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();
        }

        public static string PropertyListToString(List<float> properties)
        {
            return string.Join("|", properties.Select(p => p.ToString(CultureInfo.InvariantCulture)));
        }

        public static void AssignPropertiesToTKBlock(TKBlock tkBlock, string properties)
        {
            List<float> propertyList = PropertyStringToList(properties);           
            tkBlock.position.x = propertyList[0];
            tkBlock.position.y = propertyList[1];
            tkBlock.position.z = propertyList[2];
            tkBlock.eulerAngles.x = propertyList[3];
            tkBlock.eulerAngles.y = propertyList[4];
            tkBlock.eulerAngles.z = propertyList[5];
            tkBlock.localScale.x = propertyList[6];
            tkBlock.localScale.y = propertyList[7];
            tkBlock.localScale.z = propertyList[8];
            tkBlock.properties = propertyList;
        }
    }*/
}
