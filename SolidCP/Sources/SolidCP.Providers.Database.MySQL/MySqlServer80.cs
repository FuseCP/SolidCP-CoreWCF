﻿// Copyright (c) 2016, SolidCP
// SolidCP is distributed under the Creative Commons Share-alike license
// 
// SolidCP is a fork of WebsitePanel:
// Copyright (c) 2015, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.Win32;
//using MySql.Data.MySqlClient;
using System.IO;

using SolidCP.Providers.OS;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace SolidCP.Providers.Database
{
    public class MySqlServer80 : MySqlServer57
	{

		public MySqlServer80(): base() {	}

		public override bool IsInstalled() => IsInstalled("8.0");

		public override void CreateUser(SqlUser user, string password)
		{
			if (user.Databases == null)
				user.Databases = new string[0];

			/*if (!((Regex.IsMatch(user.Name, @"[^\w\.-]")) && (user.Name.Length > 16)))
            {
                Exception ex = new Exception("INVALID_USERNAME");
                throw ex;
            }
            */
			ExecuteNonQuery(String.Format(
								"CREATE USER '{0}'@'%' IDENTIFIED BY '{1}'",
								user.Name, password));

			if (OldPassword)
				ChangeUserPassword(user.Name, password);

			// add access to databases
			foreach (string database in user.Databases)
				AddUserToDatabase(database, user.Name);
		}

		private void AddUserToDatabase(string databaseName, string user)
		{
			// grant database access
			ExecuteNonQuery(String.Format("GRANT ALL PRIVILEGES ON {0}.* TO '{1}'@'%'",
					databaseName, user));
		}

		public override void ExecuteSqlNonQuery(string databaseName, string commandText)
		{
			commandText = "USE " + databaseName + ";\n" + commandText;
			ExecuteNonQuery(commandText);
		}

		public override void ChangeUserPassword(string username, string password)
		{
			ExecuteNonQuery(String.Format("ALTER USER '{0}'@'%' IDENTIFIED BY '{1}';",
				username, password));
		}


		private int ExecuteNonQuery(string commandText)
		{
			return ExecuteNonQuery(commandText, ConnectionString);
		}

		private int ExecuteNonQuery(string commandText, string connectionString)
		{
			MySqlConnection conn = new MySqlConnection(connectionString);
			MySqlCommand cmd = new MySqlCommand(commandText, conn);
			conn.Open();
			int ret = cmd.ExecuteNonQuery();
			conn.Close();
			return ret;
		}

	}
}
