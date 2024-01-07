namespace TrafficControlApp.Processors.Abstractions;

public interface IProcessor<TInputData>
    // where TOutputData : new()
    // where TOutputData : IProcessResult
{
    // TOutputData ProcessAsync(TInputData inputData);
    // bool CanRun { get; set; }
    //                                 
    //                                 string ProcessId { get; set; }
    //                                 
    //                                 string InputId { get; set; }
    // bool CompletedWithDependentProcessors { get; set; }
    public bool CompletedProcessing { get; set; }
    public bool HasDependants { get; }
    Task ProcessNextAsync(TInputData inputData);

    void AddDependentProcessor(IProcessor<TInputData> dependentProcessor);

    void SetDependents(Queue<IProcessor<TInputData>> dependents);

    Queue<IProcessor<TInputData>> GetDependents();
    
    event NotifyNestedProcessingCompleted NestedProcessingCompleted;
    delegate void NotifyNestedProcessingCompleted();
    // IProcessor<TInput> GetNextProcessor()
}