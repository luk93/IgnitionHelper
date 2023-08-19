using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IgnitionHelper.Function_Containers
{
    public static class JsonOperations
    {
        public static string MultiplyProperties(JObject jsonObj, FileInfo jsonFile, string exportPath, string propertyToEdit, int arrayIndexToFind, StreamWriter streamWriter)
        {
            JToken? copiedtoken = null;
            string? copiedName = jsonObj["name"]?.Value<string>();
            if (copiedName == null)
                return "root json object name is null!"; 

            IEnumerable<JToken> tokens = from p in jsonObj[propertyToEdit]
                                         select p;

            //Sort just for easier reading
            tokens = tokens.OrderBy(t => Int32.Parse((t["name"] ?? 0).Value<string>()?.Split('_')[2] ?? string.Empty));

            var enumerable = tokens as JToken[] ?? tokens.ToArray();
            foreach (var token in enumerable)
            {
                try
                {
                    //Hardset for now
                    int arrayIndexFound = int.Parse((token["name"] ?? 0).Value<string>()?.Split('_')[2] ?? string.Empty);

                    if (arrayIndexFound == arrayIndexToFind)
                    {
                        copiedtoken = token[propertyToEdit];
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message + "\n" + ex.StackTrace;
                }
            }
            if (copiedtoken == null)
                return $"Node: {propertyToEdit},  Index: {arrayIndexToFind} is null!";
            
            foreach (var token in enumerable)
            {
                try
                {
                    //Hardset for now
                    int arrayIndexFound = int.Parse((token["name"] ?? 0).Value<string>()?.Split('_')[2] ?? string.Empty);

                    if (arrayIndexFound != arrayIndexToFind)
                    {
                        var tokenString = JsonConvert.SerializeObject(copiedtoken).Replace($"{copiedName}[{arrayIndexToFind}]", $"{copiedName}[{arrayIndexFound}]");
                        var newToken = JsonConvert.DeserializeObject(tokenString) as JToken;
                        if (newToken == null)
                        {
                            streamWriter.WriteLine($"\nToken[{arrayIndexFound}] was null!");
                            continue;
                        }
                        token[propertyToEdit] = newToken;
                        streamWriter.WriteLine($"\nEdited token {copiedName}[{arrayIndexFound}] -> property name: {propertyToEdit}");
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message + "\n" + ex.StackTrace;
                }
            }
            var newJsonObject = jsonObj.DeepClone();
            if (newJsonObject == null || newJsonObject[propertyToEdit] == null) return "Cloning root Json Object failed!";
            newJsonObject[propertyToEdit]?.Replace((JArray)JToken.FromObject(tokens));
            var serializedJson = JsonConvert.SerializeObject(newJsonObject);
            string newJsonPath = exportPath + @"\" + jsonFile.Name.Replace(".json", "_edit.json");
            try
            {
                //File.WriteAllText(newJsonPath, serializedTokens, Encoding.UTF8);
                File.WriteAllText(newJsonPath, serializedJson, Encoding.UTF8);
                return $"Saved new file {newJsonPath}";
            }
            catch (Exception ex)
            {
                return ex.Message +"\n"+ ex.StackTrace;
            }
        }
        public static string MultiplyTag(JObject jsonObj, FileInfo jsonFile, string exportPath, string propertyToEdit, string tagNameToMultiply, StreamWriter streamWriter)
        {
            JToken? copiedToken = null;
            List<JToken> newTokens = new();

            IEnumerable<JToken> tokens = from p in jsonObj[propertyToEdit]
                                         select p;
            List<string?> tagNames = new();
            foreach (var token in tokens)
            {
                tagNames.Add(token["name"]?.Value<string>());
                if(copiedToken == null && (token["name"]?.Value<string>() == tagNameToMultiply))
                {
                    copiedToken = token;
                }
            }
            if (copiedToken == null)
                return "Copied token is null!";
            foreach (var tagName in tagNames)
            {
                var token = copiedToken.DeepClone();
                token["name"] = tagName;
                newTokens.Add(token);
                streamWriter.WriteLine($"Added token tagName: {tagName}");
            }
            var newJsonObject = jsonObj.DeepClone();
            if (newJsonObject == null || newJsonObject[propertyToEdit] == null) return "Cloning root Json Object failed!";
            newJsonObject[propertyToEdit]?.Replace((JArray)JToken.FromObject(newTokens));
            var serializedJson = JsonConvert.SerializeObject(newJsonObject);
            string newJsonPath = exportPath + @"\" + jsonFile.Name.Replace(".json", "_edit.json");
            try
            {
                File.WriteAllText(newJsonPath, serializedJson, Encoding.UTF8);
                return $"Saved new file {newJsonPath}";
            }
            catch (Exception ex)
            {
                return ex.Message + "\n" + ex.StackTrace;
            }
        }

    }
}
