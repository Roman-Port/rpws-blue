using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LibRpws;
using System.Collections.Generic;
using System.Threading;
using RpwsBlue.Entities;
using Newtonsoft.Json;
using LibRpwsDatabase.Entities;
using RpwsBlue.MasterServer;
using Algolia.Search;

namespace RpwsBlue
{
    public delegate void HttpServiceDelegate(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee);

    public struct HttpService
    {
        public HttpServiceDelegate code;
        public string pathname;
        public bool requiresAuth;
        public bool wildcard; //(Not including ending slash) If the path continues after this directory, this will still be used.
        public float weight; //The rate limit weight. Higher values count against the ratelimit faster.
        public RpwsServiceId id;
    }

    class Program
    {
        public const string SERVER_VERISON = "2.1.0";
        public const string SERVER_DATE_CODE = "01-26-2019-a";

        public static HttpService[] services;

        public static RpwsMasterServer master_server;

        public static LibRpwsConfigFile config
        {
            get
            {
                return LibRpwsCore.config;
            }
        }

        public static string config_pathname;

        public static AlgoliaClient algolia;

        static void Main(string[] args)
        {
            string pathname = "/root/RPWS/rpws_conf.json";
            if (!File.Exists(pathname))
                pathname = "E:/RPWS_Production/config/rpws_conf.json";
            config_pathname = pathname;
            //Load config.
            LibRpwsCore.Init(pathname);

            AppstoreApi.GetAppById("539e18f21a19dec6ca0000aa");
            AppstoreApi.GetAppById("539e18f21a19dec6ca0000aa");

            //Init master server
            LibRpwsCore.Log("Starting master server...");
            master_server = new RpwsMasterServer();

            //Init Algolia
            LibRpwsCore.Log("Starting Algolia...");
            algolia = new AlgoliaClient(config.secure_creds["algolia_admin"]["appId"], config.secure_creds["algolia_admin"]["apiKey"]);

            //Write messages
            Console.WriteLine("\r\nServer " + LibRpwsCore.config.server_name + " starting. ");
            //Add services.
            services = new HttpService[] {
                CreateService(Services.TestingService.OnRequest, "/test", RpwsServiceId.test, true),
                CreateService(Services.StatsPage.OnRequest, "/info", RpwsServiceId.info, false, false),
                CreateService(Services.Login.LoginClientService.OnClientRequest, "/login/client", RpwsServiceId.loginClient, false),
                CreateService(Services.Login.LoginClientService.OnFinishRequest, "/login/client/finish", RpwsServiceId.loginClientFinish, false),
                CreateService(Services.WeatherProxy.WeatherProxy.OnClientRequest, "/v1/weather_proxy", RpwsServiceId.weatherProxy, false, false),
                CreateService(Services.Me.ApiMeService.OnClientRequest, "/v1/me", RpwsServiceId.me, false, true),
                CreateService(Services.RpwsMe.OnClientRequest, "/v1/rpws_me", RpwsServiceId.rpwsMe, false, true),
                CreateService(Services.UsersMe.ApiUsersMeService.OnClientRequest, "/v1/usersme", RpwsServiceId.usersMe, false, true),
                CreateService(Services.GetGoing.GetGoingPageService.OnClientRequest, "/getgoing/step2", RpwsServiceId.getgoingStep2, true, false),
                CreateService(Services.Timeline.ServiceTimelineApi.OnPinRequest, "/v1/user/pins", RpwsServiceId.timelinePut, true, false),
                CreateService(Services.Admin.AdminPanel.OnRequest, "/admin/", RpwsServiceId.admin, true, false),
                CreateService(Services.Admin.AdminPanel.OnSigninRequest, "/admin/signin/", RpwsServiceId.admin, true, false),
                CreateService(Services.Timeline.ServiceTimelineSyncApi.OnSyncRequest, "/v1/user/timeline/sync", RpwsServiceId.timelineSync, true, true),
                CreateService(Services.Timeline.ServiceTimelineSandboxApi.OnGenerateSandboxRequest, "/v1/timeline/get_sandbox", RpwsServiceId.timelineSandbox, true, true),
                CreateService(Services.Trends.ServiceAppstoreClickTrends.PutTrends, "/v1/trends/app_click/put", RpwsServiceId.trendsPut, false),
                CreateService(Services.Trends.ServiceAppstoreClickTrends.GetTrends, "/v1/trends/app_click/get", RpwsServiceId.trendsGet, false, false),
                CreateService(Services.OauthV2.OauthV2.BeginFrontend, "/v1/oauth2", RpwsServiceId.Oauth2Step1, false, false),
                CreateService(Services.OauthV2.OauthV2.CreateFakeUserEndpoint, "/v1/oauth2/create_fake_user", RpwsServiceId.Ouath2FakeUser, false, false),
                CreateService(Services.OauthV2.OauthV2.Step2, "/v1/oauth2/step2", RpwsServiceId.Oauth2Step2, false, false),
                CreateService(Services.OauthV2.OauthV2.FinishGrant, "/v1/oauth2/step3", RpwsServiceId.Oauth2Step3, false, false),
                CreateService(Services.Locker.LockerService.RpwsOnRequest, "/v1/locker/", RpwsServiceId.locker, false, false),
                CreateService(Services.PebbleAnalytics.PblAnalytics.OnClientRequest, "/v1/analytics/", RpwsServiceId.locker, false, false),
                //Publishing
                CreateService(Services.PublishApi.CorePublishApi.OnOldPublishEndpoint, "/publish/", RpwsServiceId.publish, true, false),
                CreateService(Services.PublishApi.CorePublishApi.OnAppRequest, "/v1/publishing/app", RpwsServiceId.publish, true, true),
                CreateService(Services.PublishApi.CorePublishApi.OnClaimRequest, "/v1/publishing/claim", RpwsServiceId.publish, true, true),
                CreateService(Services.PublishApi.CreateApp.OnRequest, "/v1/publishing/create", RpwsServiceId.publish, true, true),
                CreateService(Services.PublishApi.AppList.OnRequest, "/v1/publishing/me", RpwsServiceId.publish, true, true),
                CreateService(Services.PublishApi.CorePublishApi.OnLoginRequest, "/v1/publishing/login", RpwsServiceId.publish, true, false),
                CreateService(Services.PublishApi.CorePublishApi.OnLogoutRequest, "/v1/publishing/logout", RpwsServiceId.publish, true, false),
                CreateService(Services.PublishApi.CorePublishApi.OnCreateDevAccountRequest, "/v1/publishing/create_developer_account", RpwsServiceId.publish, true, false),
                CreateService(Services.PublishApi.AppClaimRequests.OnBeginRequest, "/v1/publishing/start_claim/", RpwsServiceId.AppClaim, true, false),
                CreateService(Services.PublishApi.AppClaimRequests.OnEndRequest, "/v1/publishing/submit_claim/", RpwsServiceId.AppClaim, true, false),
                //Appstore
                CreateService(Services.Appstore.AppstoreSearch.OnRequest, "/v2/appstore/search", RpwsServiceId.AppstoreSearch, false, false),
                CreateService(Services.Appstore.AppstoreAppQuery.OnRequest, "/v2/appstore/app/", RpwsServiceId.AppstoreApp, false, false),
                CreateService(Services.Appstore.AppstoreFrontpage.OnRequest, "/v2/appstore/frontpage/", RpwsServiceId.AppstoreFrontpage, false, false),
                
            };
            //Init app editor
            Services.PublishApi.CorePublishApi.Init();

            //Sort by length to micro-optimize loading 
            Array.Sort(services,
                delegate (HttpService x, HttpService y) { return y.pathname.Length.CompareTo(x.pathname.Length); });

            //Migration
            //Migrations.LockerMigration.MigrateLockerApps();
            //Migrations.AppstoreMigration.RunMigration();
            //Migrations.MasterMigrations._7Jan19.RunMigration();

            MainAsync().GetAwaiter().GetResult();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(OnHttpRequest);
            
        }

        public static void QuickWriteToDoc(Microsoft.AspNetCore.Http.HttpContext context, string content, string type = "text/html", int code = 200)
        {
            var response = context.Response;
            response.StatusCode = code;
            response.ContentType = type;

            //Load the template.
            string html = content;
            var data = Encoding.UTF8.GetBytes(html);
            response.ContentLength = data.Length;
            response.Body.Write(data, 0, data.Length);
            //Console.WriteLine(html);
        }

        public static void QuickWriteJsonToDoc<T>(Microsoft.AspNetCore.Http.HttpContext context, T data, int code = 200)
        {
            Program.QuickWriteToDoc(context, JsonConvert.SerializeObject(data), "application/json", code);
        }

        public static HttpService CreateService(HttpServiceDelegate del, string pathname, RpwsServiceId id, bool wildcard = true, bool requiresAuth = false, float weight = 1)
        {
            HttpService ser = new HttpService();
            ser.code = del;
            ser.pathname = pathname;
            ser.wildcard = wildcard;
            ser.requiresAuth = requiresAuth;
            ser.weight = weight;
            ser.id = id;

            return ser;
        }

        public static void LogErrorToDisk(Microsoft.AspNetCore.Http.HttpContext e, HttpSession session, Exception ex)
        {
            LogToDisk(e, session, ex.Message + " at " + ex.StackTrace, E_RPWS_StatisticObject_Status.Error);
        }

        public static void LogToDisk(Microsoft.AspNetCore.Http.HttpContext e, HttpSession session, string message, E_RPWS_StatisticObject_Status status = E_RPWS_StatisticObject_Status.Message)
        {
            //Get log level
            RpwsLogLevel l = RpwsLogLevel.Standard;
            switch(status)
            {
                case E_RPWS_StatisticObject_Status.Error: l = RpwsLogLevel.High; break;
                case E_RPWS_StatisticObject_Status.Message: l = RpwsLogLevel.Standard; break;
                case E_RPWS_StatisticObject_Status.Request: l = RpwsLogLevel.Status; break;
                case E_RPWS_StatisticObject_Status.UserError: l = RpwsLogLevel.High; break;
            }
            //Log to console.
            LibRpws.LibRpwsCore.Log(message, session, l);

            //Write
            E_RPWS_StatisticObject o = new E_RPWS_StatisticObject();
            o.ip_addr = e.Connection.RemoteIpAddress.GetAddressBytes();
            o.message = message;
            o.path = e.Request.Path.ToString();
            o.status = status;
            o.time = DateTime.UtcNow.Ticks;
            o.user_uuid = null;
            if (session.user != null)
                o.user_uuid = session.user.uuid;
            //Insert it.
            //LibRpwsCore.lite_database.GetCollection<E_RPWS_StatisticObject>("stats").Insert(o);
        }

        public static Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Check if this a heartrate.
            if(e.Request.Path.ToString().StartsWith("/heartbeat"))
            {
                Program.QuickWriteToDoc(e, "I am alive!");
                return null;
            }
            //Check if this is a prefilight
            if(FindRequestMethod(e) == RequestHttpMethod.options)
            {
                e.Response.Headers.Add("Access-Control-Allow-Origin", e.Request.Headers["origin"]);
                e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                e.Response.Headers.Add("Access-Control-Allow-Methods", "put, delete, post, get, PUT, DELETE, POST, GET");
                e.Response.Headers.Add("Access-Control-Allow-Headers", "authorization");
                Program.QuickWriteToDoc(e, "Preflight OK");
                return null;
            }

            //Set some headers
            e.Response.Headers.Add("Server", LibRpwsCore.config.server_name);

            //Authenticate.
            HttpSession s = new HttpSession(e); 

            //Find a service.
            try
            {
                //The longest paths are first, so check if it starts with any of them
                string pathname = e.Request.Path.ToString().ToLower();
                for(int i = 0; i<services.Length; i++)
                {
                    HttpService ser = services[i];
                    if(pathname.StartsWith(ser.pathname))
                    {
                        //Found service!
                        //Check if the service requires authentication.
                        if(ser.requiresAuth && s.user == null)
                        {
                            //Requires auth, but we are not authorized
                            LogToDisk(e, s, $"Couldn't handle request to {pathname} because the service requires auth.", E_RPWS_StatisticObject_Status.UserError);
                            throw new RpwsStandardHttpException("Requires Authentication", "This service requires authentication, but you are not signed in.", false, 401);
                        }
                        //Continue
                        LogToDisk(e, s, $"Served request to {ser.id.ToString()}.", E_RPWS_StatisticObject_Status.Message);
                        //RpwsLogs.LogEntireRequest(e, s);
                        ser.code(e, s);
                        return null;
                    }
                }
                //Failed.
                LogToDisk(e, s, $"Couldn't handle request to {pathname} because the service couldn't be found.", E_RPWS_StatisticObject_Status.UserError);
                throw new RpwsStandardHttpException("No Service", "Could not find a service to handle your request.", false, 404);
            }
            catch (RpwsStandardHttpException ex)
            {
                //Set request headers
                if(!e.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    if(e.Request.Headers.ContainsKey("origin"))
                        e.Response.Headers.Add("Access-Control-Allow-Origin", e.Request.Headers["origin"]);
                    else
                        e.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if(!e.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"))
                    e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                if (!e.Response.Headers.ContainsKey("Access-Control-Allow-Methods"))
                    e.Response.Headers.Add("Access-Control-Allow-Methods", "put, delete, post, get, PUT, DELETE, POST, GET");

                LibRpws.LibRpwsCore.Log("Error handling request: " + ex.http_message, s, RpwsLogLevel.High);
                Program.QuickWriteToDoc(e, ex.ToJsonString(), "application/json", ex.code);
            }
            catch (Exception ex)
            {
                //Set request headers
                if (!e.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    if (e.Request.Headers.ContainsKey("origin"))
                        e.Response.Headers.Add("Access-Control-Allow-Origin", e.Request.Headers["origin"]);
                    else
                        e.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (!e.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"))
                    e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                if (!e.Response.Headers.ContainsKey("Access-Control-Allow-Methods"))
                    e.Response.Headers.Add("Access-Control-Allow-Methods", "put, delete, post, get, PUT, DELETE, POST, GET");

                LibRpws.LibRpwsCore.Log("Error handling request: " + ex.Message+" at "+ex.StackTrace, s, RpwsLogLevel.High);
                Program.QuickWriteJsonToDoc(e, new RSHE_Output
                {
                    title = "Uncaught Exception: "+ex.Message,
                    retryable = true,
                    message = ex.Message + " at "+ex.StackTrace
                }, 500);
            }

            return null;
        }

        public static Task MainAsync()
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Parse(LibRpwsCore.config.listen_ip);
                    options.Listen(addr, LibRpwsCore.config.listen_port);
                    if (LibRpwsCore.config.listen_legacy_ssl)
                    {
                        options.Listen(addr, 443, listenOptions =>
                        {
                            listenOptions.UseHttps(LibRpwsCore.config.ssl_cert_path, "");
                        });
                    }
                })
                .UseLibuv(opts => opts.ThreadCount = 4)
                .UseStartup<Program>()
                .Build();

            return host.RunAsync();
        }

        public static RequestHttpMethod FindRequestMethod(Microsoft.AspNetCore.Http.HttpContext context)
        {
            return Enum.Parse<RequestHttpMethod>(context.Request.Method.ToLower());
        }
        public static string GetPostString(Microsoft.AspNetCore.Http.HttpContext context)
        {
            //Read stream.
            byte[] buf = new byte[(int)context.Request.ContentLength];
            context.Request.Body.Read(buf, 0, buf.Length);
            //Convert to string
            string textData = Encoding.UTF8.GetString(buf);
            return textData;
        }

        public static T GetPostBodyJson<T>(Microsoft.AspNetCore.Http.HttpContext context)
        {
            string textData = GetPostString(context);
            //Finally, parse JSON
            return JsonConvert.DeserializeObject<T>(textData);
        }

        /// <summary>
        /// Compares times and returns the "XXX ago" string.
        /// </summary>
        /// <param name="start">Date later in the past.</param>
        /// <param name="end">Date closer to now. Probably is now.</param>
        /// <returns></returns>
        public static string CompareDates(DateTime start, DateTime end)
        {
            TimeSpan length = end - start;

            if (length.TotalMinutes < 1)
                return "Now";
            if (length.TotalMinutes < 60)
                return $"{CompareDates_GrammarHelper(length.TotalMinutes, "minute")} ago";
            if (length.TotalHours < 24)
                return $"{CompareDates_GrammarHelper(length.TotalHours, "hour")} ago";
            if (length.TotalDays < 40)
                return $"{CompareDates_GrammarHelper(length.TotalDays, "day")} ago";
            //Default if it's a very long time.
            return start.ToShortDateString();
        }

        private static string CompareDates_GrammarHelper(double v, string text)
        {
            int vv = (int)v;
            if (vv == 1)
                return $"{vv} {text}";
            else
                return $"{vv} {text}s";
        }
    }

    public enum RequestHttpMethod
    {
        get,
        post,
        put,
        delete,
        options
    }
}
