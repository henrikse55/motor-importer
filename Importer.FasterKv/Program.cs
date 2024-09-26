using Importer.Converters;
using Importer.FasterKv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

XmlConverter converter = new();
XmlLogStorage xmlLogStorage = new();
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

// RandomAccessReader randomAccessReader = new(
//     loggerFactory.CreateLogger<RandomAccessReader>(),
//     xmlLogStorage
// );
XmlConverter xmlConverter = new XmlConverter(loggerFactory.CreateLogger<XmlConverter>());

DbContextOptionsBuilder<ApplicationContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
dbContextOptionsBuilder.UseNpgsql("User ID=postgres;Password=mysecretpassword;Host=localhost;Port=5432;Database=motor;Pooling=true;Include Error Detail=true;");

XmlLogProcessor logProcessor = new(
        xmlLogStorage,
        xmlConverter,
        loggerFactory.CreateLogger<XmlLogProcessor>(),
        dbContextOptionsBuilder.Options
    );

// logger.LogInformation("Starting file reading");
// await randomAccessReader.ExecuteAsync(CancellationToken.None);

logger.LogInformation("Starting Log processing");
await logProcessor.ExecuteAsync(CancellationToken.None);


// var builder = Host.CreateApplicationBuilder(args);
//
// builder.Services.AddDbContextFactory<ApplicationContext>(opt =>
// {
//     opt.UseNpgsql("User ID=postgres;Password=mysecretpassword;Host=localhost;Port=5432;Database=motor;Pooling=true;Include Error Detail=true;");
// });
// builder.Services.AddSingleton<XmlLogStorage>();
// builder.Services.AddSingleton<XmlConverter>();
//
// // builder.Services.AddHostedService<XmlLogProcessor>();
// // builder.Services.AddHostedService<RandomAccessReader>();
//
// var host = builder.Build();
// host.Run();