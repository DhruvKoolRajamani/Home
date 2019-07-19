cmake_minimum_required(VERSION 3.0.0)
project(tests VERSION 0.1.0)

set (PROJECT_INCLUDE_DIR ${PROJECT_SOURCE_DIR}/include)
set (PROJECT_SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR})

set(${PROJECT_NAME}_SRCS 
    ${PROJECT_SOURCE_DIR}/jsonTest.cpp
)

set(DEVICE_MESSAGE_LIB ${PROJECT_LIBRARY_DIR}/DeviceMessage)

include_directories("${PROJECT_BINARY_DIR}")
include_directories("${PROJECT_INCLUDE_DIR}")
include_directories("${PROJECT_LIBRARY_DIR}")
include_directories("${DEVICE_MESSAGE_LIB}")

add_executable(${PROJECT_NAME}_run ${PROJECT_SOURCE_DIR}/jsonTest.cpp)

# include_directories(include ${CMAKE_SOURCE_DIR}/libraries)