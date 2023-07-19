using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper.Data_Containers
{
    public class TagGroupPath
    {
        private string _pathToFind = string.Empty;
        public string SourcePath {get; set;}
        public string? TagGroupToFind { get; private set; }
        
        public TagGroupPath(string sourcePath)
        {
            SourcePath = sourcePath;
            _pathToFind = sourcePath;
            Update();
        }
        public void Update()
        {
            TagGroupToFind = UpdateTagGroupToFind();
            UpdatePathToFind();
        }
        private string? UpdateTagGroupToFind()
        {
            if (!SourcePath.Contains("/")) return SourcePath;
            return _pathToFind?.Split('/').FirstOrDefault();
        }
        private void UpdatePathToFind()
        {
            var resultArray = _pathToFind.Split('/', 2);
            _pathToFind = resultArray.Last();
        }
        public void ResetPathToFind()
        {
            _pathToFind = SourcePath;
        }
        public bool IsTargetTagGroup() => _pathToFind == TagGroupToFind;

    }
}
