﻿using System.Data.SqlClient;

namespace PeanutButter.Utils
{
    /// <summary>
    /// LocalDb implmentation if ILocalDbFactory
    /// </summary>
    public class LocalDbFactory: ILocalDbFactory
    {
        /// <summary>
        /// LocalDb instance, usually something like "v11.0"
        /// </summary>
        public string InstanceName { get; set; }
        private const string MasterConnectionString = @"Data Source=(localdb)\{0};Initial Catalog=master;Integrated Security=True";

        /// <summary>
        /// Default constructor for a LocalDbFactory
        /// </summary>
        public LocalDbFactory()
        {
        }

        /// <summary>
        /// Constructor for a LocalDbFactory which takes an override InstanceName to attach to
        /// </summary>
        /// <param name="instanceName"></param>
        public LocalDbFactory(string instanceName)
        {
            InstanceName = instanceName;
        }

        /// <summary>
        /// Creates the LocalDb temporary database
        /// </summary>
        /// <param name="databaseName">Name of the required database</param>
        /// <param name="databaseFile">A temporary file path which may be used as backing for the new database</param>
        public virtual void CreateDatabase(string databaseName, string databaseFile)
        {
            using (var connection = new SqlConnection(GetMasterConnectionString()))
            {
                connection.Open();
                using (SqlCommand cmd = connection.CreateCommand())
                {

                    cmd.CommandText = $"CREATE DATABASE [{databaseName}] ON (NAME = N'[{databaseName}]', FILENAME = '{databaseFile}')";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"ALTER DATABASE [{databaseName}] SET TRUSTWORTHY ON";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the connectionstring to the master database for the configured LocalDb instance,
        /// required to be able to create a new temporary LocalDb database
        /// </summary>
        /// <returns></returns>
        public string GetMasterConnectionString()
        {
            return string.Format(MasterConnectionString, 
                InstanceName ?? new LocalDbInstanceEnumerator().FindHighestDefaultInstance());
        }

    }
}
