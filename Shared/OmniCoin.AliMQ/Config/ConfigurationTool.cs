//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Configuration.Json;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;

//namespace OmniCoin.AliMQ.Config
//{
//    public class ConfigurationTool
//    {
//        public T GetAppSettings<T>(string key) where T : class, new()
//        {
//            IConfiguration config = new ConfigurationBuilder()
//            .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
//            .Build();

//            T appconfig = new ServiceCollection()
//                .AddOptions()
//                .Configure<T>(config.GetSection(key))
//                .BuildServiceProvider()
//                .GetService<IOptions<T>>()
//                .Value;

//            return appconfig;
//        }
//    }
//}
