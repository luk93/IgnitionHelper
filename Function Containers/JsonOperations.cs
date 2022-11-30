﻿using Newtonsoft.Json.Linq;
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

            IEnumerable<JToken> tokens = from p in jsonObj[propertyToEdit]
                                         select p;

            //Sort just for easier reading
            tokens = tokens.OrderBy(t => Int32.Parse(t["name"].Value<string>().Split('_')[2]));

            foreach (var token in tokens)
            {
                try
                {
                    //Hardset for now
                    int arrayIndexFound = int.Parse(token["name"].Value<string>().Split('_')[2]);

                    if (arrayIndexFound == arrayIndexToFind)
                    {
                        copiedtoken = token[propertyToEdit];
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            if (copiedtoken == null)
            {
                return $"Node: {propertyToEdit},  Index: {arrayIndexToFind} is null!";
            }
            foreach (var token in tokens)
            {
                try
                {
                    //Hardset for now
                    int arrayIndexFound = int.Parse(token["name"].Value<string>().Split('_')[2]);

                    if (arrayIndexFound != arrayIndexToFind)
                    {
                        //Hardset for now
                        var tokensName = token["name"];

                        var tokenString = JsonConvert.SerializeObject(copiedtoken).Replace($"[{arrayIndexToFind}]", $"[{arrayIndexFound}]");
                        JToken? newToken = JsonConvert.DeserializeObject(tokenString) as JToken;
                        if (newToken == null)
                        {
                            streamWriter.WriteLineAsync($"\nToken[{arrayIndexFound}] was null!");
                        }
                        else
                        {
                            token[propertyToEdit] = newToken;
                            streamWriter.WriteLineAsync($"\nEdited token[{arrayIndexFound}] -> property name: {propertyToEdit}");
                        }

                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            var newJson = jsonObj;
            var serializedTokens = JsonConvert.SerializeObject(tokens);
            //TO DO
            //newJson[propertyToEdit] = tokens as JProperty;
            //var serializedJson = JsonConvert.SerializeObject(newJson);
            string newJsonPath = exportPath + @"\" + jsonFile.Name.Replace(".json", "_edit.json");
            try
            {
                File.WriteAllText(newJsonPath, serializedTokens, Encoding.UTF8);
                return $"Saved new file {newJsonPath}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
