using OmniCoin.MiningPool.API.DataPools;
using OmniCoin.MiningPool.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;

namespace OmniCoin.MiningPool.API
{
    public class Startup
    {
        public static Func<List<string>> MinerListAction;
        public static List<Miners> Pool_Miners = null;
        public static long Pool_Miners_UpdateTime = 0;

        public Startup(IConfiguration configuration)
        {
            ServerPool.Default.Start();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region  添加SwaggerUI

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info

                {
                    Title = "OmniCoin miningPool API接口文档",
                    Version = "v1",
                    Description = "OmniCoin miningPool API接口文档",
                    TermsOfService = "None"
                });
                options.IgnoreObsoleteActions();
                options.DocInclusionPredicate((docName, description) => true);
                options.IncludeXmlComments(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "OmniCoin.MiningPool.API.xml"));
                options.DescribeAllEnumsAsStrings();
                //options.TagActionsBy(api => api.HttpMethod); //根据Http请求排序
                //options.OperationFilter<BaseController>(); // 添加httpHeader参数
            });


            #endregion
            services.AddMemoryCache();
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = $"192.168.31.25:6379,{""},defaultDatabase=6,abortConnect=False";                                                                                                             
            });
            //返回区分大小写
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(option => { option.SerializerSettings.ContractResolver = new DefaultContractResolver(); });
            //services.AddJsonRpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            #region 使用SwaggerUI

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "OmniCoin miningPool API V1");
            });



            #endregion

            app.UseMvc();
           
        }
    }
}
