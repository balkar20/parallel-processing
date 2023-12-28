using System.Threading.Tasks.Dataflow;
using AutoMapper;
using TrafficControlApp.Models;
using TrafficControlApp.Models.Results;
using TrafficControlApp.Models.Results.Analyse;
using TrafficControlApp.Models.Results.Analyse.Abstractions;
using TrafficControlApp.Processors.Abstractions;
using TrafficControlApp.Services;
using TrafficControlApp.Services.Analysers;
using TrafficControlApp.Services.Storage;

namespace TrafficControlApp.Processors;

public class VehicleTrafficProcessor(
        ISharedMemoryVehicleService sharedMemoryService,
            IVehicleAnalyzerService<IAnalysingResult> vehicleAnalyzerService, 
            IMapper mapper)
    : Processor<Track>(sharedMemoryService, vehicleAnalyzerService, mapper)
{
    
    // public VehicleTrafficProcessor(ISharedMemoryVehicleService sharedMemoryService,
    //     IVehicleAnalyzerService<IAnalysingResult> vehicleAnalyzerService, 
    //     IMapper mapper) : 
    //     base(sharedMemoryService, vehicleAnalyzerService, mapper)
    // {
    // }

    #region Protected Methods

     protected override async Task<IProcessResult> ProcessLogic(Track inputData)
        {
            var vehicles = await _sharedMemoryService.GetVehicleDataByTrackId(inputData.TrackId);
            
            foreach (var vehicle in vehicles)
            {
                var vehicleType = vehicle.VehicleType;
                
            }
            
            int bytesProcessed = 0;
            
            var testVeh = new Vehicle();
            await WorkWithDependentData(inputData.TrackId);
            var typeAnaliseResult = await vehicleAnalyzerService.Analyse(testVeh);
            var result =  _mapper.Map<VehicleTrafficProcessResult>(typeAnaliseResult);
            _sharedMemoryService.VehicleTrafficProcessResultDictionary.Add(inputData.TrackId, result);
            return result;
        }

    #endregion

   
    #region Private Methods

    private async Task WorkWithDependentData(string trackId)
    {
        _sharedMemoryService.VehicleMarkProcessResultDictionary.TryGetValue(trackId, out VehicleMarkProcessResult dependentData);
        Console.WriteLine($"DependentDta(VehicleColorStatistics) Message: {dependentData.Message}");
    }

    #endregion
}