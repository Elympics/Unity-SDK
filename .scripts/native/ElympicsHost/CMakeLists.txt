# Builds the desktop native host plugin of the Elympics match launcher.
# iOS does not use this: Xcode compiles Runtime/Launcher/Native/ElympicsHost.cpp directly.
#
#   cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
#   cmake --build build --config Release
#
# Copy the result to the package, with a .meta enabling only the matching Standalone platform/CPU:
#   Windows x64: ElympicsHost.dll    -> Runtime/Launcher/Native/Windows/x86_64/
#   Linux x64:   libElympicsHost.so  -> Runtime/Launcher/Native/Linux/x86_64/
#   macOS:       ElympicsHost.bundle -> Runtime/Launcher/Native/macOS/   (build with -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64")
# Until then, the desktop bridge detects the missing library and stays inactive.

cmake_minimum_required(VERSION 3.16)
project(ElympicsHost LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)

set(ELYMPICS_HOST_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../../Runtime/Launcher/Native")

if(APPLE)
    add_library(ElympicsHost MODULE "${ELYMPICS_HOST_SOURCE_DIR}/ElympicsHost.cpp")
    set_target_properties(ElympicsHost PROPERTIES BUNDLE TRUE BUNDLE_EXTENSION bundle)
else()
    add_library(ElympicsHost SHARED "${ELYMPICS_HOST_SOURCE_DIR}/ElympicsHost.cpp")
endif()

target_include_directories(ElympicsHost PRIVATE "${ELYMPICS_HOST_SOURCE_DIR}")

if(UNIX AND NOT APPLE)
    find_package(Threads REQUIRED)
    target_link_libraries(ElympicsHost PRIVATE Threads::Threads)
endif()
