﻿using Codebreak.Framework.Generic;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codebreak.Framework.Database
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SqlManager : Singleton<SqlManager>
    {
        /// <summary>
        /// 
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// 
        /// </summary>
        public SqlConnection Connection
        {
            get
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                return connection;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        public void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string query, object param = null) where T : DataAccessObject<T>, new()
        {
            using(var connection = Connection)
            {
                return connection.Query<T>(query, param);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject"></param>
        public bool Insert<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = Connection)
            {
                return connection.Insert<T>(dataObject) != 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject"></param>
        /// <returns></returns>
        public bool Remove<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = Connection)
            {
                return connection.Delete<T>(dataObject);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject"></param>
        public bool Update<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = Connection)
            {
                return connection.Update<T>(dataObject);
            }
        }       
    }
}
