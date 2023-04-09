use serde::{Serialize};
use serde_json::{to_vec};
use rmp_serde::{to_vec as to_rmp_vec};

// Define the enum for the serialization format
#[derive(Debug)]
#[repr(u32)]
enum SerializationFormat {
    Json = 0,
    MessagePack = 1,
}

// Define the host function that will be called from WebAssembly
#[no_mangle]
extern "C" fn ubiq(format: SerializationFormat) {
    // Create an example user object
    let user = ExampleUser {
        name: String::from("John Doe"),
        username: String::from("johndoe"),
        age: 42,
    };
    
    // Serialize the user object based on the format
    let data = match format {
        SerializationFormat::Json => to_vec(&user).unwrap(),
        SerializationFormat::MessagePack => to_rmp_vec(&user).unwrap(),
    };
    
    // Call the `CallUbiq` function with the format and data
    call_ubiq(format, data.len(), data.as_ptr());
}

// Define the function that calls the host function
fn call_ubiq(format: SerializationFormat, data_len: usize, data_ptr: *const u8) {
    // Convert the data pointer and length to a byte slice
    let data = unsafe { std::slice::from_raw_parts(data_ptr, data_len) };
    
    // Do something with the format and data
    println!("Called Ubiq with format {:?}, data length {:?} and data {:?}", format, data_len, data);
}

// Define an example user struct for serialization
#[derive(Serialize)]
struct ExampleUser {
    name: String,
    username: String,
    age: u32,
}

// Define the entry point for the program
#[no_mangle]
pub extern "C" fn call_ubiq_from_wasm(format: u32) {
    // Convert the format integer to the SerializationFormat enum
    let format_enum = match format {
        0 => SerializationFormat::Json,
        1 => SerializationFormat::MessagePack,
        _ => panic!("Unsupported serialization format"),
    };
    
    // Call the `ubiq` function with the format and data
    ubiq(format_enum);
}
