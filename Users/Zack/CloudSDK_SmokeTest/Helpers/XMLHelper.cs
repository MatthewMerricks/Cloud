using CloudApiPublic.Model;
using CloudSDK_SmopkeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CloudSDK_SmokeTest.Helpers
{
    public static class XMLHelper
    {


        ///Example Implementation:
        ///
        //GenericHolder<CLError> refToErrorHolder = ProcessingErrorHolder;
        //            XmlNode newElement = XMLHelper.ReturnNewMappingNode( smokeTestClass.InputParams.FileNameMappingFile, 
        //                                                                    string.Format("Id:{0}", random1.ToString()), 
        //                                                                    string.Format("C:\\localPath\\{0}", random2.ToString()), 
        //                                                                    string.Format("C:\\serverPath\\{0}", random2),
        //                                                                    ref refToErrorHolder);
        //XmlDocument xDoc = XMLHelper.AddMappingNodeToXDoc(smokeTestClass.InputParams.FileNameMappingFile, newElement, "/MappingRecords", ref refToErrorHolder);
        //try
        //{
        //    string savePath = smokeTestClass.InputParams.FileNameMappingFile.Replace("\"", ""); 
        //    xDoc.Save(savePath);
        //}
        //catch (Exception exception)
        //{
        //    lock (ProcessingErrorHolder)
        //    {
        //        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
        //    }
        //}    

        public static AllMappings GetMappingItems(string mappingFilePath, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            //ZW No dup file name support, this method dshould not get called. 
            throw new NotImplementedException("GetMappingItems Method of XML helper Should Not Get called.");
            AllMappings mappings = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AllMappings));
                using (XmlReader reader = XmlReader.Create(mappingFilePath))
                {
                    mappings = (AllMappings)serializer.Deserialize(reader);
                }
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return mappings;
        }

        public static XmlNode ReturnNewMappingNode(string mappingFilePath, string id, string localPath, string serverPath, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            XmlDocument xDoc = new XmlDocument();
            XmlNode newElement = null;
            try
            {
                if (mappingFilePath.Contains("\""))
                    mappingFilePath = mappingFilePath.Replace("\"", "");

                xDoc.Load(mappingFilePath);
                XmlNode selectedNode = GetNodeByName(xDoc, "MappingRecords", ref ProcessingErrorHolder);
                XmlNode CurrentMappingRecord = selectedNode.FirstChild;
                newElement = xDoc.CreateElement("MappingRecordElement");

                XmlNode idNode = GetNodeByName(CurrentMappingRecord, "ID", ref ProcessingErrorHolder);
                if (idNode != null)
                {
                    idNode.InnerText = id;
                    XmlNode newIdNode = selectedNode.OwnerDocument.ImportNode(idNode, true);
                    newElement.AppendChild(newIdNode);
                }

                XmlNode localPathNode = GetNodeByName(CurrentMappingRecord, "LocalPath", ref ProcessingErrorHolder);
                if (localPathNode != null)
                {
                    localPathNode.InnerText = localPath;
                    XmlNode newlocalPathNode = selectedNode.OwnerDocument.ImportNode(localPathNode, true);
                    newElement.AppendChild(newlocalPathNode);
                }

                XmlNode serverPathNode = GetNodeByName(CurrentMappingRecord, "ServerPath", ref ProcessingErrorHolder);
                if (serverPathNode != null)
                {
                    serverPathNode.InnerText = serverPath;
                    XmlNode newServerPathNode = selectedNode.OwnerDocument.ImportNode(serverPathNode, true);
                    newElement.AppendChild(newServerPathNode);
                }
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return newElement;
        }

        public static XmlDocument AddMappingNodeToXDoc(string mappingFilePath, XmlNode newNode, string parentNodeString, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            XmlDocument xDoc =  new XmlDocument();
            if (mappingFilePath.Contains("\""))
                mappingFilePath = mappingFilePath.Replace("\"", "");
            try
            { 
                xDoc.Load(mappingFilePath);
                XmlNode recordsNode = GetNodeByName(xDoc, "MappingRecords", ref ProcessingErrorHolder);
                //necessary for crossing XmlDocument contexts
                XmlNode importNode = recordsNode.OwnerDocument.ImportNode(newNode, true);
                recordsNode.AppendChild(importNode);
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return xDoc;
        }

        public static XmlElement GetNodeByName(XmlDocument xDoc, string nodeName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            XmlElement returnValue = null;
            try 
            { 
                bool breakOuter = false;
                bool breakInner = false;
                foreach (var Element in xDoc.ChildNodes)
                {
                    bool isNode = Element is XmlNode;
                    if (isNode)
                    {
                        XmlNode node = Element as XmlNode;
                        if (node.HasChildNodes && node.Name != "MappingRecords")
                        {
                            foreach (XmlElement childNode in node.ChildNodes)
                            {
                                if (childNode.HasChildNodes && childNode.Name != "MappingRecords")
                                {

                                }
                                else if (childNode.Name == "MappingRecords")
                                {
                                    returnValue = childNode;
                                    breakInner = true;
                                    breakOuter = true;
                                }
                                if (breakInner)
                                    break;
                            }
                        }
                        if (breakOuter)
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return returnValue;
        }

        public static XmlNode GetNodeByName(XmlNode xNode, string nodeName, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            XmlNode returnValue = null;
            try
            {
                bool breakOuter = false;
                bool breakInner = false;
                foreach (var Element in xNode.ChildNodes)
                {
                    bool isNode = Element is XmlNode;
                    if (isNode)
                    {
                        XmlNode node = Element as XmlNode;
                        if(node.Name == nodeName)
                        {
                            returnValue = node;
                            breakInner = true;
                            breakOuter = true;
                        }
                        else if (node.HasChildNodes && node.Name != nodeName)
                        {
                            foreach (XmlNode childNode in node.ChildNodes)
                            {
                                if (childNode.HasChildNodes && childNode.Name != nodeName)
                                {

                                }
                                else if (childNode.Name == nodeName)
                                {
                                    returnValue = childNode;
                                    breakInner = true;
                                    breakOuter = true;
                                }
                                if (breakInner)
                                    break;
                            }
                        }
                        if (breakOuter)
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return returnValue;
        }
        
        public static bool SaveXMLFile(XmlDocument xDoc, string savePath, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            bool successful = false;
            try 
            {
                xDoc.Save(savePath);
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return successful;
        }
    }
}
