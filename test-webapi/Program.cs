
using Microsoft.AspNetCore.Hosting;
using NLog.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace test_webapi {
	public class Program {
		public static void Main(string[] args) {
			var builder = WebApplication.CreateBuilder(args);
			var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

			System.IO.Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "data"));

			// Add services to the container.
			builder.Services.AddCors(options => {
				options.AddPolicy(name: MyAllowSpecificOrigins,
								  policy => {
									  policy.AllowAnyHeader()
											.AllowAnyMethod()
											.AllowAnyOrigin();
								  });
			});

			builder.Services.AddLogging(logging =>
			{
				logging.ClearProviders();
				logging.SetMinimumLevel(LogLevel.Trace);
			});
			builder.Services.AddSingleton<ILoggerProvider, NLogLoggerProvider>();

			builder.Services.AddHttpClient();
			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment()) {
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseAuthorization();
			app.UseRouting();

			app.UseCors(MyAllowSpecificOrigins);

			app.MapControllers();
			app.MapDefaultControllerRoute();

			app.MapFallbackToFile("index.html");

			app.Run();
		}
	}
}
