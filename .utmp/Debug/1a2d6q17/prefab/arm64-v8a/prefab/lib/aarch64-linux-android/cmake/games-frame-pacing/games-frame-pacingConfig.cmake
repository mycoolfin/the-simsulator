if(NOT TARGET games-frame-pacing::swappy)
add_library(games-frame-pacing::swappy SHARED IMPORTED)
set_target_properties(games-frame-pacing::swappy PROPERTIES
    IMPORTED_LOCATION "/Users/michaelfinn/.gradle/caches/8.11/transforms/9e2da323fa37aa33f0b1b3514982095f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/libs/android.arm64-v8a/libswappy.so"
    INTERFACE_INCLUDE_DIRECTORIES "/Users/michaelfinn/.gradle/caches/8.11/transforms/9e2da323fa37aa33f0b1b3514982095f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

if(NOT TARGET games-frame-pacing::swappy_static)
add_library(games-frame-pacing::swappy_static STATIC IMPORTED)
set_target_properties(games-frame-pacing::swappy_static PROPERTIES
    IMPORTED_LOCATION "/Users/michaelfinn/.gradle/caches/8.11/transforms/9e2da323fa37aa33f0b1b3514982095f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/libs/android.arm64-v8a/libswappy_static.a"
    INTERFACE_INCLUDE_DIRECTORIES "/Users/michaelfinn/.gradle/caches/8.11/transforms/9e2da323fa37aa33f0b1b3514982095f/transformed/jetified-games-frame-pacing-2.1.2/prefab/modules/swappy_static/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

