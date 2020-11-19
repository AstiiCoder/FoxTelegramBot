using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTeleBo
    {
    class Class_DB_Operations
        {
        public static string ServerName;
        public static string DatabaseName;
        public static string connectionString = "";
        public static int LastMesIDFromDB = 0;

        private static void SetConnectionString()
            {
            connectionString = "Data Source=" + ServerName + ";Initial Catalog=" + DatabaseName + ";Integrated Security=True";
            }

        /// <summary>
        /// Use this method to change main access for all departs
        /// </summary>
        /// <param name="P1"></param>
        /// <returns>On success return R=1, and in bedly case R=0 or 2.</returns>
        public static void EditAccess(int P1, ref byte R)
            {
            SetConnectionString();
            // название процедуры
            string sqlExpression = "PR_COMMON";

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    R=2;
                    return;
                    }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // входной параметр 1 - код действия
                SqlParameter IDParam = new SqlParameter
                    {
                    ParameterName = "@ID",
                    Value = 110
                    };
                command.Parameters.Add(IDParam);
                // входной параметр 2 
                SqlParameter P1Param = new SqlParameter
                    {
                    ParameterName = "@P1",
                    Value = P1
                    };
                command.Parameters.Add(P1Param);
                R = 0;

                using (connection)
                    {
                    try
                        {
                        // так как у нас проcтой результат возвращается из процедуры, то применяем ExecuteScalar
                        int orez = (int)command.ExecuteScalar();
                        if (orez == 1) R = 1;
                        }
                    catch
                        {
                        // это значит не удалось запустить процедуру sql или получить результат
                        R = 2;
                        }
                    }
                connection.Close();
                }

            }

        /// <summary>
        /// Use this method to change delail access for the depart
        /// </summary>
        /// <param name="P1"></param>
        /// <returns>On success return R=1, and in bedly case R=0 or 2.</returns>
        public static void EditAccess(string P1, ref byte R)
            {
            SetConnectionString();
            // название процедуры
            string sqlExpression = "PR_COMMON";

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    R = 2;
                    return;
                    }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // входной параметр 1 - код действия
                SqlParameter IDParam = new SqlParameter
                    {
                    ParameterName = "@ID",
                    Value = 123
                    };
                command.Parameters.Add(IDParam);
                // входной параметр 2 
                SqlParameter P1Param = new SqlParameter
                    {
                    ParameterName = "@P1",
                    Value = P1
                    };
                command.Parameters.Add(P1Param);
                R = 0;

                using (connection)
                    {
                    try
                        {
                        // так как у нас проcтой результат возвращается из процедуры, то применяем ExecuteScalar
                        int orez = (int)command.ExecuteScalar();
                        if (orez == 1) R = 1;
                        }
                    catch
                        {
                        // это значит не удалось запустить процедуру sql или получить результат
                        R = 2;
                        }
                    }
                connection.Close();
                }

            }

        /// <summary>
        /// Use this method to change access for current, next or both years
        /// </summary>
        /// <param name="P"></param>
        /// <returns>On success return R=1, and in bedly case R=0 or 2.</returns>
        public static void EditYearAccess(string P, ref byte R)
            {
            SetConnectionString();
            // название процедуры
            string sqlExpression = "SET_VAR";

            sbyte var_value;
            switch (P)
                {
                case "Разрешить Все года":
                    var_value = 1;
                    break;
                case "Разрешить только Текущий":
                    var_value = 2;
                    break;
                case "Разрешить только Следующий":
                    var_value = 3;
                    break;
                default:
                    var_value = -2;
                    break;
                }

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    R = 2;
                    return;
                    }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                //параметры: @var_name varchar(50) = '', @var_value varchar(1000) = '', @caption varchar(50) = null, @res int =null output
                // входной параметр 1 - имя глобальной переменной
                SqlParameter var_name = new SqlParameter
                    {
                    ParameterName = "@var_name",
                    Value = "EDIT_ONE_YEAR_ONLY"
                    };
                command.Parameters.Add(var_name);
                // входной параметр 2 - значение переменной 
                SqlParameter Pvar_value = new SqlParameter
                    {
                    ParameterName = "@var_value",
                    Value = Convert.ToString(var_value)
                    };
                command.Parameters.Add(Pvar_value);
                // входной параметр 3 - значение переменной 
                SqlParameter caption = new SqlParameter
                    {
                    ParameterName = "@caption",
                    Value = ""
                    };
                command.Parameters.Add(caption);
                // определяем выходной параметр
                SqlParameter Pres = new SqlParameter
                    {
                    ParameterName = "@res",
                    SqlDbType = SqlDbType.Int // тип параметра
                    };
                // указываем, что параметр будет выходным
                Pres.Direction = ParameterDirection.Output;
                command.Parameters.Add(Pres);
                R = 0;

                using (connection)
                    {
                    try
                        {
                        command.ExecuteNonQuery();
                        int orez = Convert.ToInt32(command.Parameters["@res"].Value);
                        if (orez == 1) R = 1;
                        }
                    catch
                        {
                        // это значит не удалось запустить процедуру sql или получить результат
                        R = 2;
                        }
                    }
                connection.Close();
                }

            }


        /// <summary>
        /// Use this method to get delailized departs list with access to its 
        /// </summary>
        /// <returns>On success return text from some lines</returns>
        public static string ShowAccess()
            {
            string sqlExpression = "PR_COMMON";

            SetConnectionString();
            string returned_str = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    return "";
                    }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                // указываем, что команда представляет хранимую процедуру
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // параметр для кода
                SqlParameter nameParam = new SqlParameter
                    {
                    ParameterName = "@ID",
                    Value = 8
                    };
                // добавляем параметр
                command.Parameters.Add(nameParam);


                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    {
                    //шапка, если нужна
                    string s = "";
                    //returned_str = reader.GetName(0) + " " + reader.GetName(2) + " " + reader.GetName(3) + " " + reader.GetName(4) + "\n";
                    while (reader.Read())
                        {
                        string type_post = reader.GetString(reader.GetOrdinal("По типу поставки"));
                        string depart = reader.GetString(reader.GetOrdinal("Департамент"));
                        //приём полей с возможными значениями "null"
                        string napr = (!reader.IsDBNull(reader.GetOrdinal("Агент"))) ? reader.GetString(reader.GetOrdinal("Агент")) : " ";
                        napr += (!reader.IsDBNull(reader.GetOrdinal("Наименование фирмы покупателя"))) ? reader.GetString(reader.GetOrdinal("Наименование фирмы покупателя")) : " ";
                        string razresh = (Convert.ToString(reader.GetInt32(reader.GetOrdinal("Разреш.изм.план"))) == "1") ? char.ConvertFromUtf32(0x2714) : char.ConvertFromUtf32(0x274C); //"+" : "-";
                        string razresh_ind = (!reader.IsDBNull(reader.GetOrdinal("Разреш.инд."))) ? reader.GetString(reader.GetOrdinal("Разреш.инд.")) : " ";
                        if (razresh_ind.Length > 1) razresh_ind = "(только " + razresh_ind + ")";
                        s = type_post.Substring(0, 3) + " " + (depart.PadRight(10)).Substring(0, 8).Trim() + " " + napr.Trim() + " " + razresh + " " + razresh_ind + "\n";
                        returned_str += s;
                        }
                    reader.Close();
                    }               
                }
            return returned_str;
            }


        /// <summary>
        /// Use this method to get list messages from DB 
        /// </summary>
        /// <returns>On success return text from several lines</returns>
        public static string GetMessages(int LastMesID)
            {
            string sqlExpression = "PR_COMMON";

            SetConnectionString();
            string returned_str = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    return "";
                    }               
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                // указываем, что команда представляет хранимую процедуру
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // параметр код ветви процедуры
                SqlParameter nameParam = new SqlParameter
                    {
                    ParameterName = "@ID",
                    Value = 27
                    };
                command.Parameters.Add(nameParam);
                // параметр - номер с которого слать сообщения
                SqlParameter P1 = new SqlParameter
                    {
                    ParameterName = "@P1",
                    Value = Convert.ToString(LastMesID)
                    };
                command.Parameters.Add(P1);

                try
                    {
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        {
                        while (reader.Read())
                            {
                            returned_str += (!reader.IsDBNull(reader.GetOrdinal("MESSAGE_TEXT"))) ? reader.GetString(reader.GetOrdinal("MESSAGE_TEXT")) : "Пустое сообщение";
                            returned_str += "\n\n";
                            LastMesIDFromDB = reader.GetInt32(reader.GetOrdinal("ID_MES"));
                            }
                        reader.Close();
                        }
                    
                    }
                catch
                    {
                    return "";
                    }                  
                
                }
            return returned_str;
            }

        /// <summary>
        /// Use this method to show or update status of invoice
        /// </summary>
        /// <param name="P1"></param>
        /// <returns>On success return description of rezult</returns>
        public static string UnlockZak(string Nom, int Mode)
            {
            SetConnectionString();
            // название процедуры, пример запуска: EXEC BUSY_ZAK @nom = '14-51290', @mode = 0   
            string sqlExpression = "BUSY_ZAK";

            using (SqlConnection connection = new SqlConnection(connectionString))
                {
                try
                    {
                    connection.Open();
                    }
                catch
                    {
                    return "";
                    }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // входной параметр 1 - код действия
                SqlParameter NomZak = new SqlParameter
                    {
                    ParameterName = "@nom",
                    Value = Nom
                    };
                command.Parameters.Add(NomZak);
                // входной параметр 2 
                SqlParameter ModeAction = new SqlParameter
                    {
                    ParameterName = "@mode",
                    Value = Mode
                    };
                command.Parameters.Add(ModeAction);

                string returned_str = "";
               
                if (Mode == 0) //режим чтения заказов
                    {
                    var reader = command.ExecuteReader();
                    try
                        {
                        if (reader.HasRows)
                            {
                            string s = "";
                            while (reader.Read())
                                {
                                string nom_zak = (!reader.IsDBNull(1)) ? Convert.ToString(reader.GetValue(1)) : "";
                                string date_zak = (!reader.IsDBNull(2)) ? Convert.ToString(reader.GetValue(2)) : "";
                                string pokupatel = (!reader.IsDBNull(3)) ? Convert.ToString(reader.GetValue(3)) : "";
                                string kto = (!reader.IsDBNull(4)) ? Convert.ToString(reader.GetValue(4)) : "";
                                string kogda = (!reader.IsDBNull(5)) ? Convert.ToString(reader.GetValue(5)) : "";
                                string status = (!reader.IsDBNull(6)) ? Convert.ToString(reader.GetValue(6)) : "";
                                // соединение срок
                                string[] values = new string[] { nom_zak, " _", date_zak, pokupatel, kto, kogda, status, "_\n" };
                                s = String.Join(" ", values);
                                }
                            returned_str = "*Заблокированные заказы:*\n" + s;
                            reader.Close();
                            }
                        else
                            {
                            return "";
                            }
                        }
                    catch
                        {
                        return "";
                        }                    
                    }
                else // режим разблокировки заказов
                    try
                        {
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            {
                            while (reader.Read())
                                {
                                returned_str = (!reader.IsDBNull(0)) ? Convert.ToString(reader.GetValue(0)) : "";
                                }
                            reader.Close();
                            }

                        }
                    catch
                        {
                        return "";
                        }
                return returned_str;                                    
                }

            }


        }
    }
