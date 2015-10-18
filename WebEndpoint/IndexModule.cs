namespace WebEndpoint
{
    using Nancy;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Options["/"] = parameters =>
            {
                return this.Response.AsJson(Request)
                      .WithHeader("Access-Control-Allow-Origin", "*")
                      .WithHeader("Access-Control-Allow-Methods", "POST")
                      .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
            };

            Get["/"] = parameters =>
            {
                return View["index"];
            };
        }
    }
}