cmake_minimum_required(VERSION 3.0.0)
project(Kitchen VERSION 0.1.0)


set(PROJECT_SOURCE_DIR ${CMAKE_SOURCE_DIR}/src)
set(PROJECT_LIBRARY_DIR ${CMAKE_SOURCE_DIR}/libraries)

set(CMAKE_BINARY_DIR ${CMAKE_SOURCE_DIR}/build)
 
set(EXECUTABLE_OUTPUT_PATH ${CMAKE_BINARY_DIR})
set(LIBRARY_OUTPUT_PATH ${CMAKE_BINARY_DIR})
 
include_directories("${PROJECT_LIBRARY_DIR}")
include_directories("${PROJECT_SOURCE_DIR}")
 
add_subdirectory(${PROJECT_SOURCE_DIR}/tests)
# add_subdirectory(project2)
