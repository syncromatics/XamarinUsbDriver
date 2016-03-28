$: << '.'

require 'bundler/setup'
require 'albacore'

Dir["build/config/*.rb"].each { |file| require file }
Dir["build/tasks/*.rb"].each { |file| require file }

@config = Syncro::Config.new(ENV["BUILDCONFIGURATION"], ENV["BUILDENVIRONMENT"])
@paths = Syncro::Paths.new(Dir.pwd, @config.build_config, @config.build_env)

desc "Build Solution"
task :default => [:gen_shared_asm_info, :build] do
  puts "##teamcity[buildNumber '#{get_build_num}']"
end

assemblyinfo :gen_shared_asm_info do |asm, args|
  build_num = get_build_num()
  args.with_defaults(:dep_build_num => build_num)
  build_num_no_rev = build_num
  asm.version = build_num_no_rev
  asm.file_version = asm.version
  asm.description = args.dep_build_num
  asm.input_file = "build/SharedAssemblyInfo_Template.cs"
  asm.output_file = "build/SharedAssemblyInfo.cs"
end
