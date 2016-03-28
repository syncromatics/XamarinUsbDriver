$: << "..\\"

module Syncro
  class Config
    attr_reader :is_loaded

    def initialize(build_config, build_env)
      @build_config = build_config
      @build_config = "Debug" if @build_config == nil

      @build_env = build_env
      @build_env = "dev" if @build_env == nil
      
      validate_env(@build_env)
    end

    def validate_env(build_env)
      if(!["dev", "ci", "test", "qa", "staging", "production"].include?(build_env))
        throw "Invalid Environment: #{build_env}"
      end
    end

    def build_config
      @build_config
    end

    #dev, qa, staging, prod
    def build_env
      @build_env
    end
  end
end
