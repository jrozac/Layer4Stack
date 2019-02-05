namespace Layer4Stack.DataProcessors
{

    /// <summary>
    /// Data processor provider interface
    /// </summary>
    public interface IDataProcessorProvider
    {
        /// <summary>
        /// Gets new data processor 
        /// </summary>
        IDataProcessor New { get; }
    }
}
