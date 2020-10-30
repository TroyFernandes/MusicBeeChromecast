namespace GoogleCast.Models.Media
{
    /// <summary>
    /// Queue change type
    /// </summary>
    public enum QueueChangeType
    {
        /// <summary>
        /// Insert
        /// </summary>
        Insert = 0,

        /// <summary>
        /// Remove
        /// </summary>
        Remove = 1,

        /// <summary>
        /// Items Change
        /// </summary>
        ItemsChange = 2,

        /// <summary>
        /// Update
        /// </summary>
        Update = 3,

        /// <summary>
        /// No Change
        /// </summary>
        NoChange = 4
    }
}
