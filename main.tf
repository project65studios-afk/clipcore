terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = "us-east-1"
}

resource "aws_apprunner_service" "project65_v2" {
  service_name = "project65-web-v2"

  source_configuration {
    authentication_configuration {
      access_role_arn = "arn:aws:iam::016981601583:role/service-role/AppRunnerECRAccessRole"
    }

    image_repository {
      image_identifier      = "016981601583.dkr.ecr.us-east-1.amazonaws.com/project65-web:v10.10"
      image_repository_type = "ECR"
      image_configuration {
        port = "8080"
        runtime_environment_variables = {
          PORT = "8080"
        }
      }
    }
    auto_deployments_enabled = true
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 5
    timeout             = 2
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  instance_configuration {
    cpu               = "1024"
    memory            = "2048"
    instance_role_arn = "arn:aws:iam::016981601583:role/project65-app-runner-role"
  }

  tags = {
    DeploymentTool = "Terraform"
    Project        = "Project65"
  }


}

output "service_url" {
  value = "https://${aws_apprunner_service.project65_v2.service_url}"

}

resource "aws_apprunner_custom_domain_association" "project65_domain" {
  domain_name = "project65video.com"
  service_arn = aws_apprunner_service.project65_v2.arn
  enable_www_subdomain = true
}

output "dns_target" {
  value = aws_apprunner_custom_domain_association.project65_domain.dns_target
}

output "certificate_validation_records" {
  value = aws_apprunner_custom_domain_association.project65_domain.certificate_validation_records
}
