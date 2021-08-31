using System.Data;
using System.Data.SqlClient;

namespace ASPNET.StarterKit.Portal
{
    public class PortalDb : IPortalDb
    {
        private readonly string _connectionString;

        public PortalDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region IPortalDb Members

        /// <summary>
        ///   Delete information in the Database relating to Module being deleted in Configuration
        /// </summary>
        public void DeleteModule(int moduleId)
        {
            // Create Instance of Connection and Command Object
            var myConnection = new SqlConnection(_connectionString);
            var myCommand = new SqlCommand("Portal_DeleteModule", myConnection);

            // Mark the Command as a SPROC
            myCommand.CommandType = CommandType.StoredProcedure;

            // Add Parameters to SPROC
            var parameterModuleId = new SqlParameter("@ModuleID", SqlDbType.Int, 4);
            myConnection.Open();

            parameterModuleId.Value = moduleId;
            myCommand.Parameters.Add(parameterModuleId);

            // Open the database connection and execute the command
            myCommand.ExecuteNonQuery();
            myConnection.Close();
        }

        #endregion
    }
}