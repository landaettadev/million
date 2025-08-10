variable "project_name" { type = string default = "million" }
variable "environment"  { type = string default = "prod" }
variable "location"     { type = string default = "eastus" }
variable "mongo_connection_string" { type = string description = "MongoDB Atlas connection string" sensitive = true }
variable "jwt_secret" { type = string sensitive = true }


