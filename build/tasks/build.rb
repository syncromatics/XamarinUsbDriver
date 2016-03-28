desc "Build entire solution"
msbuild :default => [:init, :restore] do |msb|
  puts @config.build_config
  msb.properties :configuration => @config.build_config
  msb.targets :Rebuild
  msb.solution = @paths.master_solution_file
  msb.log_level = :verbose
  msb.command = "C:/Program Files (x86)/MSBuild/14.0/Bin/msbuild.exe"
end

desc "Restore nuget packages. cinst nuget.commandline if you don't have nuget.exe on path"
exec :restore do |cmd, args|
  cmd.command = "nuget"
  cmd.parameters = ["restore"]
  cmd.log_level = :verbose
end

exec :restore_components do |cmd|
  cmd.command = @paths.xamarin_restore_location
  cmd.parameters = ["restore", @paths.master_solution_file]
  cmd.log_level = :verbose
end

task :init do |t|
  puts "Initializing Artifact folders!"
  #delete folder and all contents
  FileUtils.mkdir_p(@paths.artifacts)  unless File.exist? @paths.artifacts
  #recreate it
  FileUtils.mkdir_p(@paths.nunit_results_file) unless File.exist? @paths.nunit_results_file

  #clean
  FileUtils.rm_rf(@paths.artifacts)

  FileUtils.mkdir_p(@paths.artifacts)
  FileUtils.mkdir_p(@paths.packages)
  FileUtils.mkdir_p(@paths.nunit_results_file)
  FileUtils.mkdir_p(@paths.artifacts_nuget)
  puts "DONE Initializing Artifact folders!"
end

def get_build_num
  buildNumber = `git describe --tags --always --long`.split('-')[1]
  "#{get_tag}.#{buildNumber}"
end

def get_tag
  tag = `git describe --abbrev=0 --tags`
  tag = tag.gsub(/\n/,"")
end

def get_branch
  branch = `git rev-parse --abbrev-ref HEAD`
  branch = branch.gsub(/\n/,"")
end

def get_commit_count
  count = `git rev-list --count #{get_branch}`
  count = count.gsub(/\n/,"")
end
