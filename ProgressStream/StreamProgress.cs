namespace ProgressStream
{
    /// <summary>
    /// Represents progress information reported by a <see cref="ProgressStream"/> read or write operation.
    /// </summary>
    /// <param name="BytesTransferred">The number of bytes transferred in the current operation.</param>
    /// <param name="TotalBytesTransferred">The cumulative number of bytes transferred so far.</param>
    /// <param name="TotalLength">The total length of the stream if known; otherwise, <c>null</c>.</param>
    public readonly record struct StreamProgress(
        long BytesTransferred,
        long TotalBytesTransferred,
        long? TotalLength = null)
    {
        /// <summary>
        /// Gets the completion percentage (0.0 to 100.0) if <see cref="TotalLength"/> is known and greater than zero; otherwise, <c>null</c>.
        /// </summary>
        public double? Percentage =>
            TotalLength is > 0
                ? (double)TotalBytesTransferred / TotalLength.Value * 100.0
                : null;
    }
}
