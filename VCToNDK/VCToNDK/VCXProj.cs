using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
namespace VCToNDK
{
    public class VCXProj
    {
        private Dictionary<string, List<FileInfo>> allCodeFileInfo = new Dictionary<string, List<FileInfo>>();
        private HashSet<String> visitedProjFiles = new HashSet<string>();
        private HashSet<String> visitedCodeFiles = new HashSet<string>();
        private Uri curUri = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);

        public VCXProj()
        {
        }

        public void Load(String filename)
        {
            navigate(filename);
            String path = Path.Combine(".\\", "..\\");
            Uri uri = curUri;
            Uri uriFile = new Uri(filename);
            Uri relativeUri = uri.MakeRelativeUri(uriFile);
        }

        public void Generate(String templateName = "")
        {
            if(string.IsNullOrEmpty(templateName))
            {
                templateName = "Android.mk.template";
            }

            String templateString = File.ReadAllText(templateName);
            String srcString = buildSrcString();
            templateString = templateString.Replace("{srcfile}", srcString);
            File.WriteAllText("Android.mk", templateString);
        }

        private String buildSrcString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var fileListPair in allCodeFileInfo)
            {
                String varName = fileListPair.Key.ToUpper() + "_FILES";
                sb.Append(varName + " := ");
                foreach(var file in fileListPair.Value)
                {
                    String fullName = file.FullName;
                    String relativeName = getRelativePath(fullName);
                    sb.Append(relativeName);
                    sb.AppendLine(" \\ ");
                }

                sb.AppendFormat("LOCAL_SRC_FILES += $({0}) \n", varName);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private String getRelativePath(String fileFullName)
        {
            Uri fileUri = new Uri(fileFullName);
            Uri relativeUri = this.curUri.MakeRelativeUri(fileUri);
            return relativeUri.ToString();
        }

        private void navigate(String filename)
        {
            if(!File.Exists(filename))
            {
                Console.WriteLine(filename + " is not existed.");
                return;
            }

            FileInfo info = new FileInfo(filename);
            if(visitedProjFiles.Contains(info.FullName))
            {
                return;
            } 
            else
            {
                visitedProjFiles.Add(info.FullName);
            }

            XDocument xdoc = XDocument.Load(filename);
            XNamespace Namespace = xdoc.Root.Name.Namespace;
            String originDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(info.Directory.FullName);

            // load code files
            var codeFiles = xdoc.Descendants(Namespace + "ClCompile");
            List<FileInfo> fileInfoList = new List<FileInfo>();
            foreach (XElement e in codeFiles)
            {
                XAttribute attr = e.Attribute("Include");
                if (attr != null)
                {
                    String codefile = attr.Value;
                    if(codefile.EndsWith(".cc") ||
                        codefile.EndsWith(".c") ||
                        codefile.EndsWith(".cpp"))
                    {
                        FileInfo codeInfo = new FileInfo(codefile);
                        if(!visitedCodeFiles.Contains(codeInfo.FullName))
                        {
                            visitedCodeFiles.Add(codeInfo.FullName);
                            fileInfoList.Add(codeInfo);
                        }
                    }
                }
            }

            if(fileInfoList.Count > 0)
            {
                String projName = getProjName(xdoc);
                while(allCodeFileInfo.Keys.Contains(projName))
                {
                    projName += "X";
                }

                allCodeFileInfo.Add(projName, fileInfoList);
            }

            var projRefs = xdoc.Root.Descendants(Namespace + "ProjectReference");
            foreach (XElement e in projRefs)
            {
                XAttribute attr = e.Attribute("Include");
                if (attr != null)
                {
                    navigate(attr.Value);
                }
            }

            Directory.SetCurrentDirectory(originDir);
        }

        private String getProjName(XDocument xdoc)
        {
            XNamespace ns = xdoc.Root.Name.Namespace;
            var elems = xdoc.Descendants(ns + "RootNamespace");
            foreach(var elem in elems)
            {
                if(!String.IsNullOrEmpty(elem.Value))
                {
                    return elem.Value;
                }
            }

            throw new InvalidDataException("proj doesn't have name: " + xdoc.ToString());
        }
    }
}
