nuspec :generate_nuspec do |nuspec|
    nuspec.id = "XamarinUsbDriver"
    nuspec.version = "0.0.0"
    nuspec.authors = ["syncromatics"]
    nuspec.description = ""
    nuspec.output_file = @paths.driver_nuspec
    GetProjectDependencies(@paths.driver_folder).each do |dep|
        nuspec.dependency dep.Name, dep.Version
    end
    nuspec.file @paths.driver_dll, "lib\\dnx451"
end

desc "Pack nupkg to local artifacts folder"
exec :pack_nuget, [:version] => [:default, :generate_nuspec] do |cmd, args|
  args.with_defaults(:version => get_build_num())
  cmd.command = "/usr/bin/nuget"
  cmd.parameters = [
    "pack",
    "#{@paths.driver_nuspec}",
    "-OutputDirectory \"#{@paths.artifacts_nuget}\""]
end

class Dependency
    attr_accessor :Name, :Version

    def new(name, version)
        @Name = name
        @Version = version
    end
end

def GetProjectDependencies(project)
    path = "#{project}/packages.config"
    packageDep = Array.new

    if File.exists? path
        packageConfigXml = File.read("#{project}/packages.config")
        doc = REXML::Document.new(packageConfigXml)
        doc.elements.each("packages/package") do |package|
            dep = Dependency.new
            dep.Name = package.attributes["id"]
            dep.Version = package.attributes["version"]
            packageDep << dep
        end
    end

    packageDep
end
