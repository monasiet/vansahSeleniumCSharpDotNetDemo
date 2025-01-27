﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using System;
using System.Buffers.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Vansah
{
    public class VansahNode
    {


        //--------------------------- ENDPOINTS -------------------------------------------------------------------------------
        private static string api_Version = "v1";
        private static string vansah_Url = "https://prod.vansahnode.app";
        private static string add_Test_Run = vansah_Url + "/api/" + api_Version + "/run";
        private static string add_Test_Log = vansah_Url + "/api/" + api_Version + "/logs";
        private static string update_Test_Log = vansah_Url + "/api/" + api_Version + "/logs/";
        private static string remove_Test_Log = vansah_Url + "/api/" + api_Version + "/logs/";
        private static string remove_Test_Run = vansah_Url + "/api/" + api_Version + "/run/";
        private static string test_Script = vansah_Url + "/api/" + api_Version + "/testCase/list/testScripts";
        //--------------------------------------------------------------------------------------------------------------------


        //--------------------------- INFORM YOUR UNIQUE VANSAH TOKEN HERE ---------------------------------------------------
        private static string vansah_Token = "Your Token Here";


        //--------------------------- INFORM IF YOU WANT TO UPDATE VANSAH HERE -----------------------------------------------
        // 0 = NO RESULTS WILL BE SENT TO VANSAH
        // 1 = RESULTS WILL BE SENT TO VANSAH
        private static readonly string updateVansah = "1";
        //--------------------------------------------------------------------------------------------------------------------	


        //--------------------------------------------------------------------------------------------------------------------
        private string testFolders_Id;  //Mandatory (GUID Test folder Identifer) Optional if issue_key is provided
        private string jira_Issue_Key;  //Mandatory (JIRA ISSUE KEY) Optional if Test Folder is provided
        private string sprint_Name; //Mandatory (SPRINT KEY)
        private string case_Key;   //CaseKey ID (Example - TEST-C1) Mandatory
        private string release_Name;  //Release Key (JIRA Release/Version Key) Mandatory
        private string environment_Name; //Enivronment ID from Vansah for JIRA app. (Example SYS or UAT ) Mandatory
        private string result_Name;    // Result Key such as (Result value. Options: (0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested)) Mandatory
        private bool send_Screenshot;   // true or false If Required to take a screenshot of the webPage that to be tested.
        private string comment;  //Actual Result 	
        private int step_Order;   //Test Step index	
        private string test_Run_Identifier; //To be generated by API request
        private string test_Log_Identifier; //To be generated by API request
        private string File;
        private int testRows;
        private HttpClient httpClient;



        //------------------------ VANSAH INSTANCE CREATION---------------------------------------------------------------------------------
        //Creates an Instance of vansahnode, to set all the required field
        public VansahNode(string testFolders, string jiraIssue)
        {
            testFolders_Id = testFolders;
            jira_Issue_Key = jiraIssue;

        }
        //Default Constructor
        public VansahNode()
        {
        }

        //------------------------ VANSAH Add TEST RUN(TEST RUN IDENTIFIER CREATION) -------------------------------------------
        //POST prod.vansahnode.app/api/v1/run --> https://apidoc.vansah.com/#0ebf5b8f-edc5-4adb-8333-aca93059f31c
        //creates a new test run Identifier which is then used with the other testing methods: 1) Add_test_log 2) remove_test_run

        //For JIRA ISSUES
        public void AddTestRunFromJiraIssue(string testCase)
        {

            case_Key = testCase;
            send_Screenshot = false;

            ConnectToVansahRest("AddTestRunFromJiraIssue", null);
        }
        //For TestFolders
        public void AddTestRunFromTestFolder(string testCase)
        {

            case_Key = testCase;
            send_Screenshot = false;
            ConnectToVansahRest("AddTestRunFromTestFolder", null);
        }
        //------------------------------------------------------------------------------------------------------------------------



        //-------------------------- VANSAH Add TEST LOG (LOG IDENTIFIER CREATION ------------------------------------------------
        //POST prod.vansahnode.app/api/v1/logs --> https://apidoc.vansah.com/#8cad9d9e-003c-43a2-b29e-26ec2acf67a7
        //Adds a new test log for the test case_key. Requires "test_run_identifier" from Add_test_run

        public void AddTestLog(string result, string Comment, int testStepRow, bool sendScreenShot, IWebDriver driver)
        {

            result_Name = result.ToLower();
            comment = Comment;
            step_Order = testStepRow;
            send_Screenshot = sendScreenShot;
            ConnectToVansahRest("AddTestLog", driver);
        }
        //-------------------------------------------------------------------------------------------------------------------------



        //------------------------- VANSAH Add QUICK TEST --------------------------------------------------------------------------
        //POST prod.vansahnode.app/api/v1/run --> https://apidoc.vansah.com/#0ebf5b8f-edc5-4adb-8333-aca93059f31c
        //creates a new test run and a new test log for the test case_key. By calling this endpoint, 
        //you will create a new log entry in Vansah with the respective overal Result. 
        //(0 = N/A, 1= FAIL, 2= PASS, 3 = Not Tested). Add_Quick_Test is useful for test cases in which there are no steps in the test script, 
        //where only the overall result is important.

        //For JIRA ISSUES
        public void AddQuickTestFromJiraIssue(string testCase, string result)
        {

            //0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested
            case_Key = testCase;
            result_Name = result.ToLower();
            send_Screenshot = false;
            ConnectToVansahRest("AddQuickTestFromJiraIssue", null);
        }
        //For TestFolders
        public void AddQuickTestFromTestFolders(string testCase, string result)
        {

            //0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested
            case_Key = testCase;
            result_Name = result.ToLower();
            send_Screenshot = false;

            ConnectToVansahRest("AddQuickTestFromTestFolders", null);
        }

        //------------------------------------------------------------------------------------------------------------------------------


        //------------------------------------------ VANSAH REMOVE TEST RUN *********************************************
        //POST prod.vansahnode.app/api/v1/run/{{test_run_identifier}} --> https://apidoc.vansah.com/#2f004698-34e9-4097-89ab-759a8d86fca8
        //will delete the test log created from Add_test_run or Add_quick_test

        public void RemoveTestRun()
        {
            send_Screenshot = false;
            ConnectToVansahRest("RemoveTestRun", null);
        }
        //------------------------------------------------------------------------------------------------------------------------------

        //------------------------------------------ VANSAH REMOVE TEST LOG *********************************************
        //POST remove_test_log https://apidoc.vansah.com/#789414f9-43e7-4744-b2ca-1aaf9ee878e5
        //will delete a test_log_identifier created from Add_test_log or Add_quick_test

        public void RemoveTestLog()
        {
            send_Screenshot = false;
            ConnectToVansahRest("RemoveTestLog", null);
        }
        //------------------------------------------------------------------------------------------------------------------------------


        //------------------------------------------ VANSAH UPDATE TEST LOG ------------------------------------------------------------
        //POST update_test_log https://apidoc.vansah.com/#ae26f43a-b918-4ec9-8422-20553f880b48
        //will perform any updates required using the test log identifier which is returned from Add_test_log or Add_quick_test

        public void UpdateTestLog(string result, string Comment, bool sendScreenShot, IWebDriver driver)
        {

            result_Name = result.ToLower();
            comment = Comment;
            send_Screenshot = sendScreenShot;
            ConnectToVansahRest("UpdateTestLog", driver);
        }

        private void ConnectToVansahRest(string type, IWebDriver driver)
        {

            if (updateVansah == "1")
            {
                httpClient = new HttpClient();
                HttpResponseMessage response = null;
                JsonObject requestBody;
                HttpContent Content;

                //Adding headers
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", vansah_Token);
                if (send_Screenshot)
                {
                     Screenshot TakeScreenshot = ((ITakesScreenshot)driver).GetScreenshot();
                     File = TakeScreenshot.AsBase64EncodedString;
                   
                }
                if (type == "AddTestRunFromJiraIssue")
                {

                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", JiraIssueAsset());
                    if (Properties().Count != 0) { requestBody.Add("properties", Properties()); }

                    httpClient.BaseAddress = new Uri(add_Test_Run);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "AddTestRunFromTestFolder")
                {
                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", TestFolderAsset());
                    if (Properties().Count != 0) { requestBody.Add("properties", Properties()); }

                    //Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(add_Test_Run);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;


                }
                if (type == "AddTestLog")
                {
                    requestBody = AddTestLogProp();
                    if (send_Screenshot)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);


                    }

                    httpClient.BaseAddress = new Uri(add_Test_Log);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "AddQuickTestFromJiraIssue")
                {

                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", JiraIssueAsset());
                    if (Properties().Count != 0)
                    {
                        requestBody.Add("properties", Properties());
                    }
                    requestBody.Add("result", resultObj(result_Name));
                    if (send_Screenshot)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }

                    httpClient.BaseAddress = new Uri(add_Test_Run);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);

                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "AddQuickTestFromTestFolders")
                {
                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", TestFolderAsset());
                    if (Properties().Count != 0)
                    {
                        requestBody.Add("properties", Properties());
                    }
                    requestBody.Add("result", resultObj(result_Name));
                    if (send_Screenshot)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }

                    httpClient.BaseAddress = new Uri(add_Test_Run);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "RemoveTestRun")
                {

                    httpClient.BaseAddress = new Uri(remove_Test_Run + test_Run_Identifier);
                    response = httpClient.DeleteAsync("").Result;
                }
                if (type == "RemoveTestLog")
                {

                    httpClient.BaseAddress = new Uri(remove_Test_Log + test_Log_Identifier);
                    response = httpClient.DeleteAsync("").Result;
                }
                if (type == "UpdateTestLog")
                {
                    requestBody = new();

                    requestBody.Add("result", resultObj(result_Name));
                    requestBody.Add("actualResult", comment);
                    if (send_Screenshot)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }

                    httpClient.BaseAddress = new Uri(update_Test_Log + test_Log_Identifier);
                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PutAsync("", Content).Result;
                }
                if (response.IsSuccessStatusCode)
                {

                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                    var obj = JObject.Parse(responseMessage);

                    if (type == "AddTestRunFromJiraIssue")
                    {

                        test_Run_Identifier = obj.SelectToken("data.run.identifier").ToString();
                        Console.WriteLine($"Test Run has been created Successfully RUN ID : {test_Run_Identifier}");

                    }
                    if (type == "AddTestRunFromTestFolder")
                    {
                        test_Run_Identifier = obj.SelectToken("data.run.identifier").ToString();
                        Console.WriteLine($"Test Run has been created Successfully RUN ID : {test_Run_Identifier}");
                    }
                    if (type == "AddTestLog")
                    {
                        test_Log_Identifier = obj.SelectToken("data.log.identifier").ToString();
                        Console.WriteLine($"Test Log has been Added to a test Step Successfully LOG ID : {test_Log_Identifier}");

                    }
                    if (type == "AddQuickTestFromJiraIssue")
                    {

                        string message = obj.SelectToken("message").ToString();
                        Console.WriteLine($"Quick Test : {message}");

                    }
                    if (type == "AddQuickTestFromTestFolders")
                    {

                        string message = obj.SelectToken("message").ToString();
                        Console.WriteLine($"Quick Test : {message}");

                    }
                    if (type == "RemoveTestLog")
                    {
                        Console.WriteLine($"Test Log has been removed from a test Step Successfully LOG ID : {test_Log_Identifier}");
                    }
                    if (type == "RemoveTestRun")
                    {
                        Console.WriteLine($"Test Run has been removed Successfully for the testCase : {case_Key} RUN ID : {test_Run_Identifier}");

                    }
                    response.Dispose();

                }
                else
                {
                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                    var obj = JObject.Parse(responseMessage);
                    Console.WriteLine(obj.SelectToken("message").ToString());
                    response.Dispose();
                }

            }
            else
            {
                Console.WriteLine("Sending Test Results to Vansah TM for JIRA is Disabled");
            }
        }
        //Setter and Getter's 
        //To Set the TestFolderID 
        public void SetTestFolders_Id(string testFolders_Id)
        {
            this.testFolders_Id = testFolders_Id;
        }

        //To Set the JIRA_ISSUE_KEY
        public void SetJira_Issue_Key(string jira_Issue_Key)
        {
            this.jira_Issue_Key = jira_Issue_Key;
        }

        //To Set the SPRINT_NAME
        public void SetSprint_Name(string sprint_Name)
        {
            this.sprint_Name = sprint_Name;
        }

        //To Set the RELEASE_NAME
        public void SetRelease_Name(string release_Name)
        {
            this.release_Name = release_Name;
        }

        //To Set the ENVIRONMENT_NAME
        public void SetEnvironment_Name(string environment_Name)
        {
            this.environment_Name = environment_Name;
        }

        //JsonObject - Test Run Properties 
        private JsonObject Properties()
        {
            JsonObject environment = new();
            environment.Add("name", environment_Name);

            JsonObject release = new();
            release.Add("name", release_Name);

            JsonObject sprint = new();
            sprint.Add("name", sprint_Name);

            JsonObject Properties = new();
            if (sprint_Name != null)
            {
                if (sprint_Name.Length >= 2)
                {
                    Properties.Add("sprint", sprint);
                }
            }
            if (release_Name != null)
            {
                if (release_Name.Length >= 2)
                {
                    Properties.Add("release", release);
                }
            }
            if (environment_Name != null)
            {
                if (environment_Name.Length >= 2)
                {
                    Properties.Add("environment", environment);
                }
            }

            return Properties;
        }


        //JsonObject - To Add TestCase Key
        private JsonObject TestCase()
        {

            JsonObject testCase = new();
            if (case_Key != null)
            {
                if (case_Key.Length >= 2)
                {
                    testCase.Add("key", case_Key);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid TestCase Key");
            }

            return testCase;
        }
        //JsonObject - To Add Result ID
        private JsonObject resultObj(string result)
        {

            JsonObject resultID = new();

            resultID.Add("name", result);


            return resultID;
        }
        //JsonObject - To Add JIRA Issue name
        private JsonObject JiraIssueAsset()
        {

            JsonObject asset = new();
            if (jira_Issue_Key != null)
            {
                if (jira_Issue_Key.Length >= 2)
                {
                    asset.Add("type", "issue");
                    asset.Add("key", jira_Issue_Key);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid JIRA Issue Key");
            }


            return asset;
        }
        //JsonObject - To Add TestFolder ID 
        private JsonObject TestFolderAsset()
        {

            JsonObject asset = new();
            if (testFolders_Id != null)
            {
                if (testFolders_Id.Length >= 2)
                {
                    asset.Add("type", "folder");
                    asset.Add("identifier", testFolders_Id);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid TestFolder ID");
            }


            return asset;
        }

        //JsonObject - To AddTestLog
        private JsonObject AddTestLogProp()
        {

            JsonObject testRun = new();
            testRun.Add("identifier", test_Run_Identifier);

            JsonObject stepNumber = new();
            stepNumber.Add("number", step_Order);

            JsonObject testResult = new();
            testResult.Add("name", result_Name);

            JsonObject testLogProp = new();

            testLogProp.Add("run", testRun);

            testLogProp.Add("step", stepNumber);

            testLogProp.Add("result", testResult);

            testLogProp.Add("actualResult", comment);


            return testLogProp;
        }
        //JsonObject - To Add Add Attachments to a Test Log
        private JsonObject AddAttachment(string file)
        {

            JsonObject attachmentsInfo = new();
            attachmentsInfo.Add("name", file);
            attachmentsInfo.Add("extension", "png");
            attachmentsInfo.Add("file", File);

            return attachmentsInfo;

        }

        //Set FileName
        private string FileName()
        {

            string filename = Path.GetRandomFileName().Replace(".", "");

            return filename;
        }


    }
}