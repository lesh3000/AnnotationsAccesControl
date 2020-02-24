using GdPicture14;
using GdPicture14.WEB;
using GdPicture14.Annotations;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Web;
using System.Web.Http;

namespace DocuViewareREST
{
    public class WebApiApplication : HttpApplication
    {
        public static readonly int SESSION_TIMEOUT = 20; //Set to 20 minutes. Use -1 to handle DocuVieware session timeout through ASP.NET session mechanism.
        private static readonly bool STICKY_SESSION = true; //Set false to use DocuVieware on Servers Farm with non sticky sessions.
        private const DocuViewareSessionStateMode DOCUVIEWARE_SESSION_STATE_MODE = DocuViewareSessionStateMode.InProc; //Set DocuViewareSessionStateMode.File is STICKY_SESSION is False.

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            DocuViewareManager.SetupConfiguration(STICKY_SESSION, DOCUVIEWARE_SESSION_STATE_MODE, GetCacheDirectory());
            DocuViewareLicensing.RegisterKEY("0488386897882708674261864"); //Unlocking DocuVieware. Please insert your demo or commercial license key here.
            DocuViewareEventsHandler.NewDocumentLoaded += NewDocumentLoadedHandler;
            DocuViewareEventsHandler.CustomAction += Dispatcher;
        }

        private static string GetCacheDirectory()
        {
            return HttpRuntime.AppDomainAppPath + "\\Cache";
        }

        class annot
        {
            public string id;
            public int pageNo;
        }
        class details
        {
            public annot annot;
            public string type;
        }

        class load
        {
            public string UserType;
            public string Path;

        }
        private static void Dispatcher(object sender, CustomActionEventArgs e)
        {
            GdPicturePDF oPdf = new GdPicturePDF();
            switch (e.actionName){

                //loading document and setting annotations access control
                //the workflow is designed for PDF files, you can apply similar workflow on the other formats using GdPictureImaging class
                case "load":
                    load oLoad = JsonConvert.DeserializeObject<load>(e.args.ToString());
                    GdPictureStatus status=oPdf.LoadFromFile(HttpRuntime.AppDomainAppPath + "\\Files\\" + oLoad.Path,true);
                    AnnotationManager oAnnotationmanager = new AnnotationManager();
                    oAnnotationmanager.InitFromGdPicturePDF(oPdf);
                        for (int i = 1; i < oAnnotationmanager.PageCount; i++)
                        {
                            oAnnotationmanager.SelectPage(i);
                            for (int y = 0; y < oAnnotationmanager.GetAnnotationCount(); y++)
                            {
                                Annotation annot = oAnnotationmanager.GetAnnotationFromIdx(y);
                            if (oLoad.UserType == "user")//case for external user member
                            {
                                if (annot.Tag != oLoad.UserType)//annotation is not vissible if not added by external group memebr
                                {
                                    annot.Visible = false;
                                }
                            }
                            else if(oLoad.UserType == "staff")//case for staff member
                            {
                                annot.Visible = true;//all annot are visible
                            }

                            }
                            oAnnotationmanager.SaveAnnotationsToPage();
                    }
                    e.docuVieware.LoadFromGdPicturePdf(oPdf);
                    e.docuVieware.DisplayPage(1);

                    break;


                //setting annotation tag that represents access group and saving annotation to PDF
                //the workflow is designed for PDF files, you can apply similar workflow on the other formats using GdPictureImaging class

                case "setAnnotationTag":
                    
                    e.docuVieware.GetNativePDF(out oPdf);
                    AnnotationManager manager = new AnnotationManager();
                    manager.InitFromGdPicturePDF(oPdf);
                    
                    details oDetails = JsonConvert.DeserializeObject<details>(e.args.ToString());
                    manager.SelectPage(oDetails.annot.pageNo);
                    int pages = manager.GetAnnotationCount();
                    for (int i = 0; i < pages; i++)
                    {
                        Annotation oAnnotation = manager.GetAnnotationFromIdx(i);
                        if (oAnnotation.Guid == oDetails.annot.id)
                        {
                            oAnnotation.Tag = oDetails.type;
                        }
                    }
                    manager.SaveAnnotationsToPage();
         
                    status= oPdf.SaveToFile(HttpRuntime.AppDomainAppPath + "\\Files\\DocuViewareFlyer.pdf",true);
                    

                    break;
            }
        }

            private static void NewDocumentLoadedHandler(object sender, NewDocumentLoadedEventArgs e)
        {
            e.docuVieware.PagePreload = e.docuVieware.PageCount <= 50 ? PagePreloadMode.AllPages : PagePreloadMode.AdjacentPages;
        }
    }
}
