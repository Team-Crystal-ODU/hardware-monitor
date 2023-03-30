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
        System.Timers.Timer timer = new System.Timers.Timer(60000);
        Console.WriteLine("Timer enabled at " + DateTime.Now); // Debug statement
        timer.Elapsed += (source, e) => OnTimedEvent(Path.Combine(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "values"), "values.json"), timer);
        timer.AutoReset = true;
        timer.Enabled = true;
        Console.WriteLine("Timer enabled successfully"); // Debug statement

        while (!cancellationToken.IsCancellationRequested)
        {
            //its ugly, but these lines create the values folder and the values.json file
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "values"));

            Monitor(computer, Path.Combine(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "values"), "values.json"));

            await Task.Delay(1000);
        }

        computer.Close();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }



    public static void Monitor(Computer computer, string file)
    {


        //computer.Accept(new UpdateVisitor());



        foreach (IHardware hardware in computer.Hardware)
        {
           
            // Console.WriteLine("Hardware: {0}", hardware.Name);

            /* foreach (IHardware subhardware in hardware.SubHardware)
             {
                 Console.WriteLine("\tSubhardware: {0}", subhardware.Name);

                 foreach (ISensor sensor in subhardware.Sensors)
                 {
                     Console.WriteLine("Sensor: {0}, value: {1}", sensor.Name, sensor.Value);
                 }
             }
            */
            // if (File.Exists(file) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(file)).TotalMinutes > maxFileAgeInMinutes)
            // {
            // Delete the file
            //      File.Delete(file);
            //   }


            foreach (ISensor sensor in hardware.Sensors)
            {
                string str = sensor.Name.Substring(0, 4);
                DateTimeOffset now;

                //string content;
                //string content2 = "";

                
                //creating JSON object to store data in
                object content = null;


                //only grab power stats, ignore individual core temps

                if (sensor.SensorType.ToString() == "Power" && str != "Core")
                {
                    Console.WriteLine("Sensor: {0}, value: {1} W", sensor.Name, sensor.Value);

                    //grab cpu wattage for json file
                    if (sensor.Name == "Package")
                    {
                        now = (DateTimeOffset)DateTime.UtcNow.ToLocalTime();

                        //content = "\t\"cpu_watts\": "  + sensor.Value + ",";
                        // content2 = "{\n\t\"timestamp\": " + now.ToString("yyyy-MM-ddTHH:mm:ss") + ",";

                        //creating json object to turn into json data later
                        content = new
                        {
                            timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss") + ",",
                            cpu_watts = sensor.Value
                        };
                    }

                    //grab gpu wattage for json file
                    else
                    {
                        //content = "\t\"gpu_watts\": " + sensor.Value + "\n},";

                        //creating json object to turn into json data later
                        content = new
                        {
                            gpu_watts = sensor.Value
                        };
                    }


                    //if file is empty, start w/ heading
                    // if (new FileInfo(file).Length == 0)
                    /*  if (!File.Exists(file))
                      {                    
                          using (StreamWriter writer = new StreamWriter(file))
                          {
                              // Write content to the file if necessary
                              writer.WriteLine("{\"data\": [");
                              if (content2 != "")
                              {
                                  writer.WriteLine(content2);
                              }
                              writer.WriteLine(content);
                          }
                      }

                      //if file has content, append it
                      else
                      {
                          using (StreamWriter writer = new StreamWriter(file, append: true))
                          {
                              if (content2 != "")
                              {
                                  writer.WriteLine(content2);
                              }
                              writer.WriteLine(content);
                          }*/






                    if (content != null)
                    {
                        string jsonContent = JsonSerializer.Serialize(content);

                        // Add the content to the file if it exists, or create a new file if it doesn't
                        using (StreamWriter writer = File.Exists(file) ? File.AppendText(file) : File.CreateText(file))
                        {
                            if (File.Exists(file) && writer.BaseStream.Length > 0)
                            {
                               //  Add a comma before the new object if the file already contains objects
                                writer.WriteLine(",");
                            }

                            writer.WriteLine(jsonContent);
                        }
                    }


                    //if (File.Exists(file) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(file)).TotalMinutes > maxFileAgeInMinutes)
                    //{
                   //     File.Delete(file);
                  //  }
                }
              

            }
        }
        computer.Accept(new UpdateVisitor());
    }
    private static void OnTimedEvent(string file, System.Timers.Timer timer)
    {
        Console.WriteLine("Deleting file at " + DateTime.Now); // Debug statement

        // Check if the file exists and is too old
        if (File.Exists(file))
        {
            // Delete the file
            File.Delete(file);
        }

        Console.WriteLine("File deleted at " + DateTime.Now); // Debug statement
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