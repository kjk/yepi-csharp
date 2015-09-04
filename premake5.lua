workspace "All"
  configurations { "Debug", "Release" }
  platforms { "Any CPU" }
  flags { "Symbols" }
  location "ivalid_location"
  filter "action:vs*"
    location "."
  filter {}

  startproject "Tests"

  project "Yepi"
    kind "SharedLib"
    language "C#"
    files { "utils/*.cs" }
    links {
      "System", "Microsoft.VisualBasic", "System.Drawing", "System.Windows",
      "System.Windows.Forms", "PresentationCore", "PresentationFramework",
      "System.Runtime.Remoting", "WindowsBase"
    }

  project "Tests"
    kind "ConsoleApp"
    language "C#"
    files { "tests/*.cs" }
    links { "System" }
    dependson { "Yepi" }
