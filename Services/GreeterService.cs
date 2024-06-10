using Grpc.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using GrpcServer;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace GrpcServer.Services
{


    public class GreeterService : UserService.UserServiceBase
    {

        private readonly ILogger<GreeterService> _logger;
        private static RabbitMqServer _rabbitMqClient;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)//O método Login verifica se o Username contém "Cl" (cliente) ou "Ad" (administrador) e faz a validação do usuário com base no conteúdo dos arquivos CSV correspondentes.
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");
            string arquivoAdmins = @Path.Combine(pastaFicheiros, "AdminUsers.csv");

            int i = 0;

            if (request.Username.Contains("Cl"))
            {
                List<string> UserData = FileReader(arquivoClients);

                if (UserData == null)
                {
                    return Task.FromResult(new LoginResponse
                    {
                        Type = LoginResponse.Types.UserType.NotFound,
                        Message = "Error: Server couldn't find data"
                    });
                }
                while (UserData[i] != "Empty")
                {
                    if (UserData[i] == request.Username)
                    {
                        if (UserData[i + 1] == request.Password)
                        {

                            return Task.FromResult(new LoginResponse
                            {
                                Type = LoginResponse.Types.UserType.Motorcycle,
                                Message = "Client found. Welcome back " + request.Username,
                                AssignedService = UserData[i + 2]

                            });
                        }
                    }
                    i+=3;
                }
                return Task.FromResult(new LoginResponse
                {
                    Type = LoginResponse.Types.UserType.NotFound,
                    Message = "Client not found"
                });
            }
            else
            {
                if (request.Username.Contains("Ad"))
                {
                    List<string> UserData = FileReader(arquivoAdmins);

                    if (UserData == null)
                    {
                        return Task.FromResult(new LoginResponse
                        {
                            Type = LoginResponse.Types.UserType.NotFound,
                            Message = "Error: Server couldn't find data"
                        });
                    }
                    while (UserData[i] != "Empty")
                    {
                        if (UserData[i] == request.Username)
                        {
                            if (UserData[i + 1] == request.Password)
                            {
                                return Task.FromResult(new LoginResponse
                                {
                                    Type = LoginResponse.Types.UserType.Administrator,
                                    Message = "Administrator found. Welcome back " + request.Username,
                                });
                            }
                        }
                        i +=2;
                    }
                    return Task.FromResult(new LoginResponse
                    {
                        Type = LoginResponse.Types.UserType.NotFound,
                        Message = "Administrator not found"
                    });
                }
            }
            return Task.FromResult(new LoginResponse
            {
                Type = LoginResponse.Types.UserType.NotFound,
                Message = "Error: Invalid Username (Username must have 'Cl' as a client or 'Ad' as a administrator)"
            });
        }

        public override Task<CheckUserResponse> CheckUserExists(CheckUserRequest request, ServerCallContext context)//O método CheckUserExists verifica se o Username existe no arquivo Alocacao_Cliente_Servico.csv.
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            int i = 0;
            List<string> UserData = FileReader(arquivoClients);
            
            if (UserData == null)
            {
                return Task.FromResult(new CheckUserResponse
                {
                    Exists = true
                });
            }
            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.Username)
                {
                    return Task.FromResult(new CheckUserResponse
                    {
                        Exists = true
                    });
                }
                i += 3;
               
            }
            return Task.FromResult(new CheckUserResponse
            {
                Exists = false
            });
        }

        public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)//O método Register registra um novo usuário no arquivo Alocacao_Cliente_Servico.csv, verificando primeiro se o Username contém "Cl_".
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            int i = 0;

            if(!request.Username.Contains("Cl_"))
            {
                return Task.FromResult(new RegisterResponse
                {
                    Success = false,
                    Message = "Error: Username must contain 'Cl_'."
                });
            }

            List<string> UserData = FileReader(arquivoClients);
            AddUser(arquivoClients, UserData, request);

            

            if(AddUser(arquivoClients, UserData, request) == true)
            {
                return Task.FromResult(new RegisterResponse
                {
                    Success = true,
                    Message = "User has been added."
                });
            }
            else
            {
                return Task.FromResult(new RegisterResponse
                {
                    Success = false,
                    Message = "Error: User has not been added."
                });
            }

        }

        public override Task<TaskResponse> RequestNewTask(TaskRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, request.Service+".csv");

            List<string> ServiceData = FileReader(arquivoService);

            int i = 0;

            if(ServiceData != null)
            {
                // Descobre se o user tem ja uma task designada
                while (ServiceData[i] != "Empty")
                {
                    if (ServiceData[i+3] == request.Username && ServiceData[i+2] != "Concluido")
                    {
                        return Task.FromResult(new TaskResponse
                        {
                            TaskDescription = ServiceData[i + 1]
                        });
                    }
                    i += 4;
                }
                i = 0;
                //Designa a nova task para o user
                while (ServiceData[i] != "Empty")
                {
                    if (ServiceData[i + 3] == "Cl_NotAttributed")
                    {
                        ServiceData[i + 3] = request.Username;
                        ServiceData[i + 2] = "Em curso";
                        UpdateService(arquivoService, ServiceData);
                        return Task.FromResult(new TaskResponse
                        {
                            TaskDescription = ServiceData[i + 1]
                        });
                    }
                    i += 4;
                }
            }
            return Task.FromResult(new TaskResponse { 
                TaskDescription = "Error: Service not found"
            });

        }

        public override Task<TaskCompletionResponse> CompleteTask(TaskCompletionRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, request.Service + ".csv");

            List<string> ServiceData = FileReader(arquivoService);

            int i = 0;
            if (ServiceData != null)
            {
                // Descobre se o user tem ja uma task designada mesmma ideologia
                while (ServiceData[i] != "Empty")
                {
                    if (ServiceData[i + 1] == request.TaskDescription)
                    {
                        ServiceData[i + 2] = "Concluido";
                        UpdateService(arquivoService, ServiceData);
                        return Task.FromResult(new TaskCompletionResponse
                        {
                            Message = "Service update: Task has been saved as completed"
                        });
                    }
                    i += 4;
                }
            }

            return Task.FromResult(new TaskCompletionResponse
            {
                Message = "Error: Service not found"
            });
        }

        public override Task<ServiceAssignmentResponse> AssignToService(ServiceAssignmentRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            List<string> UserData = FileReader(arquivoClients);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.Username)
                {
                    UserData[i + 2] = "Servico_" + request.ServiceSymbol;
                    bool update = UpdateUser(arquivoClients, UserData);

                    if (update = true)
                    {
                        return Task.FromResult(new ServiceAssignmentResponse
                        {
                            Message = "You are now assigned to service " + request.ServiceSymbol
                        });
                    }
                    else
                    {
                        return Task.FromResult(new ServiceAssignmentResponse
                        {
                            Message = "Error: Couldn't save assignment switch"
                        });
                    }
                }
                i += 3;

            }
            return Task.FromResult(new ServiceAssignmentResponse
            {
                Message = "Error: User data not found"
            });
        }

        public override Task<AssignedServiceResponse> GetAssignedService(AssignedServiceRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            List<string> UserData = FileReader(arquivoClients);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.Username)
                {                                  
                    return Task.FromResult(new AssignedServiceResponse
                    {
                           ServiceSymbol = UserData[i+2]
                    });
                   
                }
                i += 3;

            }
            return Task.FromResult(new AssignedServiceResponse
            {
                ServiceSymbol = "Not found"
            });
        }

        public override Task<UnassignResponse> UnassignFromService(UnassignRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            List<string> UserData = FileReader(arquivoClients);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.Username)
                {
                    UserData[i + 2] = "Not Assigned";
                    bool update = UpdateUser(arquivoClients, UserData);

                    if (update = true)
                    {
                        return Task.FromResult(new UnassignResponse
                        {
                            Message = "You have been removed from assignment"
                        });
                    }
                    else
                    {
                        return Task.FromResult(new UnassignResponse
                        {
                            Message = "Error: Couldn't save unassignment"
                        });
                    }
                }
                i += 3;

            }
            return Task.FromResult(new UnassignResponse
            {
                Message = "Error: User data not found"
            });
        }

        public override Task<SubscribeResponse> SubscribeToService(SubscribeRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            List<string> UserData = FileReader(arquivoClients);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.Username)
                {
                    //ServiceName é o do Motciclista
                    UserData[i + 3] = request.ServiceSymbol;
                    bool update = UpdateUser(arquivoClients, UserData);

                    if (update = true)
                    {
                        return Task.FromResult(new SubscribeResponse
                        {
                            Message = "User " + request.Username + "has been assigned to service " + request.ServiceSymbol
                        });
                    }
                    else
                    {
                        return Task.FromResult(new SubscribeResponse
                        {
                            Message = "Error: Couldn't save user assignment"
                        });
                    }
                }
                i += 3;

            }
            return Task.FromResult(new SubscribeResponse
            {
                Message = "Error: User data not found"
            });
        }

        public override Task<DisplayServiceResponse> DisplayServiceData(DisplayServiceRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, "Servico_" + request.ServiceSymbol + ".csv");

            List<string> ServiceData = FileReader(arquivoService);
            string Data = "";

            int i = 0;

            if (ServiceData != null)
            {
                while (ServiceData[i] != "Empty")
                {
                    Data += "TaskID: " + ServiceData[i] + ", Description: " + ServiceData[i+1] + ", State: " + ServiceData[i+2] + ", ClientID: " + ServiceData[i+3] + "\n"; 
                    i += 4;
                }
                return Task.FromResult(new DisplayServiceResponse
                {
                    ServiceData = Data
                });
            }
            return Task.FromResult(new DisplayServiceResponse
            {
                ServiceData = "Error: Service not found"
            });
        }

        public override Task<AddServiceResponse> AddService(AddServiceRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, "Servico_" + request.ServiceName + ".csv");

            StreamWriter writer = new StreamWriter(arquivoService);

            writer.WriteLine("TarefaID,Descricao,Estado,ClienteID"); 
            writer.WriteLine("Empty,Empty,Empty,Empty");

            writer.Close();

            return Task.FromResult(new AddServiceResponse
            {
                Message = "Service has been added"
            });


        }

        public override Task<DeleteServiceResponse> DeleteService(DeleteServiceRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, "Servico_" + request.ServiceSymbol + ".csv");

            if(arquivoService != null)
            {
                File.Delete(arquivoService);
                return Task.FromResult(new DeleteServiceResponse { Message = "Service has been deleted"});
            }
            else
            {
                return Task.FromResult(new DeleteServiceResponse
                {
                    Message = "Service doesn't exist"
                });
            }
        }

        public override Task<AddTaskResponse> AddTask(AddTaskRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoService = @Path.Combine(pastaFicheiros, "Servico_" + request.ServiceSymbol + ".csv");

            List<string> ServiceData = FileReader(arquivoService);

            int i = 0;

            if (ServiceData != null)
            {
                while (ServiceData[i] != "Empty")
                {
                    i++; 
                }

                //precisa de uma maneira de gerar um unico ID
                ServiceData[i] = "SA_T6";
                ServiceData[i+1] = request.TaskDescription;
                ServiceData[i+2] = "Nao alocado";
                ServiceData[i+3] = "Cl_NotAttributed";
                ServiceData[i+4] = "Empty";
                ServiceData[i+5] = "Empty";

                if(UpdateService(arquivoService, ServiceData))
                {
                    return Task.FromResult(new AddTaskResponse { Message = "Task has been added"});
                }
            }
            return Task.FromResult(new AddTaskResponse
            {
                Message = "Error: Service not found"
            });
        }

        public override Task<AssignServiceToMotorcycleResponse> AssignServiceToMotorcycle(AssignServiceToMotorcycleRequest request, ServerCallContext context)
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoBikes = @Path.Combine(pastaFicheiros, "MotaService.csv");

            List<string> UserData = FileReader(arquivoBikes);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.MotorcycleName)
                {
                    UserData[i + 1] = request.ServiceName;
                    bool update = UpdateUser(arquivoBikes, UserData);

                    if (update = true)
                    {
                        return Task.FromResult(new AssignServiceToMotorcycleResponse
                        {
                            Message = "User " + request.MotorcycleName + "has been assigned to service " + request.ServiceName
                        });
                    }
                    else
                    {
                        return Task.FromResult(new AssignServiceToMotorcycleResponse
                        {
                            Message = "Error: Couldn't save user assignment"
                        });
                    }
                }
                i += 3;

            }
            return Task.FromResult(new AssignServiceToMotorcycleResponse
            {
                Message = "Error: User data not found"
            });
        }

        public override Task<AssignUserToServiceResponse> AssignUserToService(AssignUserToServiceRequest request, ServerCallContext context) 
        {
            string diretorioBase = @AppDomain.CurrentDomain.BaseDirectory;
            string pastaFicheiros = @Path.Combine(diretorioBase, "Ficheiros de exemplo");
            string arquivoClients = @Path.Combine(pastaFicheiros, "Alocacao_Cliente_Servico.csv");

            List<string> UserData = FileReader(arquivoClients);
            int i = 0;

            while (UserData[i] != "Empty")
            {
                if (UserData[i] == request.UserId)
                {
  
                    UserData[i + 2] = request.ServiceName;
                    bool update = UpdateUser(arquivoClients, UserData);

                    if (update = true)
                    {
                        return Task.FromResult(new AssignUserToServiceResponse
                        {
                            Message = "User "+ request.UserId+ "has been assigned to service " + request.ServiceName
                        });
                    }
                    else
                    {
                        return Task.FromResult(new AssignUserToServiceResponse
                        {
                            Message = "Error: Couldn't save user assignment"
                        });
                    }
                }
                i += 3;

            }
            return Task.FromResult(new AssignUserToServiceResponse
            {
                Message = "Error: User data not found"
            });
        }

        static List<string> FileReader(string arquivo1)
        {
            List<string> OrganizeList = new();
            if (File.Exists(arquivo1))
            {
                StreamReader reader = new(File.OpenRead(arquivo1));
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    foreach (var item in values)
                    {
                        OrganizeList.Add(item);
                    }
                }
                reader.Close();
            }

            return OrganizeList;
        }

        static bool AddUser(string arquivo, List<string> OrganizeList, RegisterRequest request)
        {
            if (File.Exists(arquivo))
            {
                StreamWriter writer = new StreamWriter(arquivo);
                int i = 0;
               
                while (OrganizeList[i] != "Empty")
                {
                    string line = OrganizeList[i] + "," + OrganizeList[i + 1] + "," + OrganizeList[i+2];
                    writer.WriteLine(line);
                    i += 3;
                }
                writer.WriteLine(request.Username + "," + request.Password + "," + "Servico_" + request.Service);
                writer.WriteLine("Empty,Empty,Empty");
                

                writer.Close();

                return true;
            }
            else
            {
                Console.WriteLine("Error: File not found");
                return false;
            }
        }

        static bool UpdateUser(string arquivo, List<string> OrganizeList)
        {
            if (File.Exists(arquivo))
            {
                StreamWriter writer = new StreamWriter(arquivo);
                int i = 0;

                while (OrganizeList[i] != "Empty")
                {
                    string line = OrganizeList[i] + "," + OrganizeList[i + 1] + "," + OrganizeList[i + 2] + OrganizeList[i + 3];
                    writer.WriteLine(line);
                    i += 4;
                }
                writer.WriteLine("Empty,Empty,Empty,Empty");


                writer.Close();

                return true;
            }
            else
            {
                Console.WriteLine("Error: File not found");
                return false;
            }
        }

        static bool UpdateService(string arquivo, List<string> OrganizeList)
        {
            if (File.Exists(arquivo))
            {
                StreamWriter writer = new StreamWriter(arquivo);
                int i = 0;

                while (OrganizeList[i] != "Empty")
                {
                    string line = OrganizeList[i] + "," + OrganizeList[i + 1] + "," + OrganizeList[i + 2] + "," + OrganizeList[i + 3];
                    writer.WriteLine(line);
                    i += 4;
                }
                writer.WriteLine("Empty,Empty,Empty,Empty");


                writer.Close();

                return true;
            }
            else
            {
                Console.WriteLine("Error: File not found");
                return false;
            }
        }
    }
}
