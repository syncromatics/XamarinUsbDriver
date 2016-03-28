$: << '.'
require 'fileutils'

module Syncro
    class Paths
        def initialize(root_path, build_config, build_env)
            @root_path = root_path
            @build_config = build_config
            @build_env = build_env
        end

        def root_path
            @root_path
        end

        def master_solution_file
            File.join(root_path, "XamarinUsbDriver.sln")
        end

        def driver_folder
            File.join(root_path, "src", "XamarinUsbDriver")
        end

        def driver_dll
            File.join(get_output_path("XamarinUsbDriver"), "XamarinUsbDriver.dll")
        end

        def driver_nuspec
            File.join(root_path, "src", "XamarinUsbDriver", "XamarinUsbDriver.nuspec")
        end

        def artifacts
            File.join(root_path, "artifacts")
        end

        def packages
            File.join(artifacts, "packages")
        end

        def nunit_results_file
            File.join(artifacts, "NUnitResults")
        end

        def get_project_root(project)
            File.join(root_path, "src", project)
        end

        def get_output_path(project)
            File.join(get_project_root(project), 'bin', @build_config)
        end

        def artifacts_nuget
            File.join(artifacts, "nuget")
        end
    end
end
