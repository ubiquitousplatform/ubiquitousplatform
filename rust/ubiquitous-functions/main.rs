use extism::*;


use axum::{
    routing::{get, post},
    http::StatusCode,
    Json, Router,
};
use serde::{Deserialize, Serialize};

// create a class with a method called get_plugin that will return an instantiated plugin
// this is a workaround to the fact that we can't use const fn in the main function



// Define a struct for Plugin
pub struct Plugin {
    // Add fields here
}

// Define a struct for PluginManager
pub struct PluginManager {
    // Add fields here
}

// Implement methods for PluginManager
impl PluginManager {
    // Define a new method to create a new PluginManager
    pub fn new() -> Self {
        Self {
            // Initialize fields here
        }
    }

    // Define the get_plugin method
    pub fn get_plugin(&self) -> Plugin {
        Plugin {
            // Initialize fields here
        }
    }
}



#[tokio::main]
async fn main() {
    // initialize tracing
    tracing_subscriber::fmt::init();

    let plugin = get_plugin();
    // build our application with a route
    let app = Router::new()
        // `GET /` goes to `root`
        .route("/", get(root))
        // `POST /users` goes to `create_user`
        .route("/users", post(|| -> create_user(plugin)));

    // run our app with hyper, listening globally on port 3000
    let listener = tokio::net::TcpListener::bind("0.0.0.0:3000").await.unwrap();
    axum::serve(listener, app).await.unwrap();
}


const fn get_plugin() -> Plugin {
    let url = Wasm::url(
        "https://github.com/extism/plugins/releases/latest/download/count_vowels.wasm"
    );
    let manifest = Manifest::new([url]);
    Plugin::new(&manifest, [], true).unwrap()
}


//static plugin: Plugin = get_plugin();
// basic handler that responds with a static string
async fn root() -> &'static str {

    println!("{}", plugin.call::<&str, &str>("count_vowels", "Hello, world!").unwrap());
    "Hello, World!"
}

async fn create_user(
    // this argument tells axum to parse the request body
    // as JSON into a `CreateUser` type
    Json(payload): Json<CreateUser>,
) -> (StatusCode, Json<User>) {
    // insert your application logic here
    let user = User {
        id: 1337,
        username: payload.username,
    };

    // this will be converted into a JSON response
    // with a status code of `201 Created`
    (StatusCode::CREATED, Json(user))
}

// the input to our `create_user` handler
#[derive(Deserialize)]
struct CreateUser {
    username: String,
}

// the output to our `create_user` handler
#[derive(Serialize)]
struct User {
    id: u64,
    username: String,
}