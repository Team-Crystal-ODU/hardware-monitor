// See https://aka.ms/new-console-template for more information
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Timers;
using System.Net.Http.Json;
using System.Net;

internal sealed class Program
{ 
    private static async Task Main(string[] args)
    {              
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ConsoleHostedService>();
            })
            .RunConsoleAsync();
    }
}

internal sealed class ConsoleHostedService : IHostedService
{
   
    const int maxFileAgeInMinutes = 1;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public ConsoleHostedService(
        ILogger<ConsoleHostedService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
        };

        computer.Open();


        while (!cancellationToken.IsCancellationRequested)
        {
            //its ugly, but these lines create the values folder and the values.json file
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "values"));

            Monitor(computer, Path.Combine(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "values"), "values.json"));

            await Task.Delay(5000);
        }

        computer.Close();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
       
        return Task.CompletedTask;
    }


    //Class to establish editable object is available for power data
    public class PowerData
    {
        public DateTimeOffset timestamp { get; set; }
        public float? cpu_watts { get; set; }
        public float? gpu_watts { get; set; }
    }
    public static void Monitor(Computer computer, string file)
    {


        computer.Accept(new UpdateVisitor());


        //creating object of powerdata class to assign values 
        PowerData dataObject = new PowerData();

   


        foreach (IHardware hardware in computer.Hardware)
        {




            foreach (ISensor sensor in hardware.Sensors)
            {
                string str = sensor.Name.Substring(0, 4);
                DateTimeOffset now;

                //variable to see if gpu has been populated yet to see whether or not to push the json object to the API
                Boolean gpuGathered;


                //creating JSON object to store data in
                //object content = null;




                //only grab power stats, ignore individual core temps

                if (sensor.SensorType.ToString() == "Power" && str != "Core")
                {
                    Console.WriteLine("Sensor: {0}, value: {1} W", sensor.Name, sensor.Value);
                    

                    //grab cpu wattage for json 
                    if (sensor.Name == "Package")
                    {
                        dataObject.timestamp=DateTimeOffset.UtcNow.ToLocalTime(); 
                        dataObject.cpu_watts=sensor.Value;
                        gpuGathered = false;

                    }
                    //grab gpu wattage for json
                    else
                    {
                        dataObject.gpu_watts=sensor.Value;
                        gpuGathered=true;   
                    }





                    //Creating POST Request to API to send GPU and CPU wattage data 
                    if(gpuGathered)
                    {
                        var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://172.18.12.16:6000/hardware");
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Method = "POST";

                        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                        {
                            string jsonContent = JsonSerializer.Serialize(dataObject);
                            streamWriter.Write(jsonContent);

                        }

                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var result = streamReader.ReadToEnd();
                            Console.WriteLine(result);
                        }
                    }

                }
            }
        }
        
    }


    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}