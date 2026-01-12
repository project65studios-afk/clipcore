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
      image_identifier      = "016981601583.dkr.ecr.us-east-1.amazonaws.com/project65-web:latest"
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


  # LOCKING TO SINGLE INSTANCE (Stability over Scale)
  # App Runner lacks Sticky Sessions. Blazor Server breaks if requests hit different instances.
  # We use a custom auto-scaling config (defined below) to force max_concurrency = 1 instance.
  auto_scaling_configuration_arn = aws_apprunner_auto_scaling_configuration_version.single_instance.arn
}

resource "aws_apprunner_auto_scaling_configuration_version" "single_instance" {
  auto_scaling_configuration_name = "single-instance-lock"
  
  # High concurrency per instance is fine (Blazor is efficient)
  max_concurrency = 100 
  
  # CRITICAL: Never scale beyond 1 instance
  max_size = 1
  min_size = 1
}

output "service_url" {
  value = "https://${aws_apprunner_service.project65_v2.service_url}"

}

resource "aws_apprunner_custom_domain_association" "project65_domain" {
  domain_name = "project65video.com"
  service_arn = aws_apprunner_service.project65_v2.arn
  enable_www_subdomain = true
}

# --- DNS Automation (Route 53) ---

# 1. Get the Hosted Zone (Assumes you bought it in this account)
data "aws_route53_zone" "primary" {
  name = "project65video.com"
}

# 2. Create the Validation Records (Solving the 24h wait!)
resource "aws_route53_record" "certificate_validation" {
  for_each = {
    for record in aws_apprunner_custom_domain_association.project65_domain.certificate_validation_records : record.name => record
  }

  zone_id = data.aws_route53_zone.primary.zone_id
  name    = each.value.name
  type    = each.value.type
  ttl     = 60
  records = [each.value.value]
  allow_overwrite = true
}

# 3. Point 'www' to the App Runner service
resource "aws_route53_record" "www" {
  zone_id = data.aws_route53_zone.primary.zone_id
  name    = "www.project65video.com"
  type    = "CNAME"
  ttl     = 300
  records = [aws_apprunner_custom_domain_association.project65_domain.dns_target]
}

# Note: App Runner does not support root domain Alias (A-record) natively in Route 53 
# without a CloudFront distribution or specific Zone ID. 
# For now, we ensure 'www' works.


output "dns_target" {
  value = aws_apprunner_custom_domain_association.project65_domain.dns_target
}

output "certificate_validation_records" {
  value = aws_apprunner_custom_domain_association.project65_domain.certificate_validation_records
}
