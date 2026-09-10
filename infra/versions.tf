terraform {
  required_version = ">= 1.6"
  required_providers {
    aws    = { source = "hashicorp/aws",    version = "~> 5.60" }
    random = { source = "hashicorp/random", version = "~> 3.6"  }
  }
}

provider "aws" {
  region = "ap-southeast-2"
  default_tags { tags = { project = "tarmac" } }
}

locals { name = "tarmac" }

resource "random_id" "sfx" { byte_length = 3 }
