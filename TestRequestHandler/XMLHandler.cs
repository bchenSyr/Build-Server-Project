/////////////////////////////////////////////////////////////////////
// XMLHandler.cs - Makes and parse test requests                  
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #3, CSE 681, Fall2017                                  
// Environment: Windows 10 Education, MacBook                                       
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Creates and parses TestRequest XML messages using XDocument
 * 
 * Public Interface:
 * ===================
 * BuildRequestProducer class:
 * RequestToXML() - build XML document that represents a test request
 * SaveXml() - save to a XML file
 * 
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Oct 29 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // BuildRequestProducer class
    public class BuildRequestProducer
    {
        public string author { get; set; } = "";
        public string dateTime { get; set; } = "";
        public string testProject { get; set; } = "";
        public string testDriver { get; set; } = "";
        public List<string> testedFiles { get; set; } = new List<string>();
        //public Queue<string> testedFiles { get; set; } = new Queue<string>();
        public XDocument doc { get; set; } = new XDocument();
        XElement testRequestElem = new XElement("testRequest");

        /*----< Method:  build XML document that represents a test request>--------------*/
        public void RequestToXML(bool IsFirstTest)
        {
            //XElement testRequestElem = new XElement("testRequest");

            if (IsFirstTest)
            {
                doc.Add(testRequestElem);

                XElement authorElem = new XElement("author");
                authorElem.Add(author);
                testRequestElem.Add(authorElem);

                XElement dateTimeElem = new XElement("dateTime");
                dateTimeElem.Add(DateTime.Now.ToString());
                testRequestElem.Add(dateTimeElem);
            }

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);

            XElement projectElem = new XElement("testProject");
            projectElem.Add(testProject);
            testElem.Add(projectElem);

            XElement driverElem = new XElement("testDriver");
            driverElem.Add(testDriver);
            testElem.Add(driverElem);

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(file);
                testElem.Add(testedElem);
            }
        }

        /*----< Method:  save TestRequest to XML file>--------------*/
        public bool SaveXml(string path)
        {
            try
            {
                doc.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

    }

    public class BuildRequestParser
    {
        public string author { get; set; } = "";
        public string dateTime { get; set; } = "";
        public string testProj { get; set; } = "";
        public string testDriver { get; set; } = "";
        public List<string> testedFiles { get; set; } = new List<string>();
        public XDocument doc { get; set; } = new XDocument();

        /*----< load TestRequest from XML file >-----------------------*/
        public List<string> ParseString(string buildRequest)
        {
            StringReader stringReader = new StringReader(buildRequest);
            XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
            xmlTextReader.MoveToContent();
            List<string> result = new List<string>();

            string elementName = "";
            string valueName = "";
            char elementType = 'i';

            int testNum = 1;

            while (xmlTextReader.Read())
            {

                if (xmlTextReader.NodeType == XmlNodeType.Element)
                {
                    elementName = xmlTextReader.Name;
                    if (elementName == "author") elementType = 'a';
                    else if (elementName == "dateTime") elementType = 'b';
                    else if (elementName == "testProject") elementType = 'c';
                    else if (elementName == "testDriver") elementType = 'd';
                    else if (elementName == "tested") elementType = 'e';
                    continue;
                }

                if (xmlTextReader.NodeType == XmlNodeType.Text)
                {
                    valueName = xmlTextReader.Value;
                    switch (elementType)
                    {
                        case 'a': result.Add(string.Format("Author: {0}", xmlTextReader.Value)); break;
                        case 'b': result.Add(string.Format("Time: {0}", xmlTextReader.Value)); break;
                        case 'c':
                            {
                                result.Add("ATest"); // add a keywork to seperate each test section in the build request
                                result.Add(testNum.ToString()); // the number of test
                                testNum++;
                                result.Add(string.Format(xmlTextReader.Value)); // test name
                                break;
                            }
                        case 'd': result.Add(xmlTextReader.Value); break; // test driver
                        case 'e': result.Add(string.Format(xmlTextReader.Value)); break; // tested files
                        default: break;
                    }
                }
            }
            return result;
        }

        /*----< load TestRequest from XML file >-----------------------*/
        public bool LoadXml(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        /*----< parse document for property value >--------------------*/
        public string Parse(string propertyName)
        {
            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "author":
                        author = parseStr;
                        break;
                    case "dateTime":
                        dateTime = parseStr;
                        break;

                    case "testProject":
                        testProj = parseStr;
                        break;

                    case "testDriver":
                        testDriver = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }

        /*----< parse document for property list >---------------------*/
        /*
		* - now, there is only one property list for tested files
		*/
        public List<string> ParseList(string propertyName)
        {
            List<string> values = new List<string>();

            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);//把propertyName放到一个枚举集合parseElems

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }
    }

    ///////////////////////////////////////////////////////////////////
    // test_TestRequest class
    class Test_TestRequest
    {
//#if (TEST_X)
        static void Main(string[] args)
        {
            PrintTool.title("Testing Method of TestRequestProducer class: RequestToXML()");
            string savePath = "../../../RepoStorage/BuildRequests/";
            string fileName = "TestTestReqHandler.xml";

            if (!System.IO.Directory.Exists(savePath))
                System.IO.Directory.CreateDirectory(savePath);

            string fileSpec = System.IO.Path.Combine(savePath, fileName);
            fileSpec = System.IO.Path.GetFullPath(fileSpec);

            BuildRequestProducer tr = new BuildRequestProducer();
            tr.author = "Beier Chen";
            tr.testDriver = "td1.cs";
            tr.testedFiles.Add("tf1.cs");
            tr.testedFiles.Add("tf2.cs");
            tr.testedFiles.Add("tf3.cs");
            bool JustTest = true;
            tr.RequestToXML(JustTest);
            Console.Write("\n{0}", tr.doc.ToString());

            PrintTool.title("Testing Method of TestRequestProducer class: SaveXml()");
            Console.Write("\n\n saving to \"{0}\"", fileSpec);
            tr.SaveXml(fileSpec);
            Console.Write("\n reading from \"{0}\"", fileSpec);
            PrintTool.title ("End of the demonstration");
        }
    }
//#endif
 }

