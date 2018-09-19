const express = require('express')
const app = express()
const homeController = require('./controller/home-controller')

app.set('port', (process.env.PORT || 5000))
app.use(express.static(__dirname + '/public'))
app.set("view engine", "ejs")
app.set("views", "./views")

app.get('/', homeController.getHomeView)

/* const pg = require('pg')
const config = {
  user: 'bcfodeyhmhsrcn',
  host: 'ec2-54-197-230-161.compute-1.amazonaws.com',
  database: 'd9lmukb58d9f3g',
  password: '39021d98ad2a8aeaffda294b550bd73f560e423afba332b26e7c925dcf846a21',
  port: 5432,
  ssl: true,
}
const pool = new pg.Pool(config)

app.get('/', function(request, response) {
  pool.connect(function(err, client, done) {
    if(err) {
      return console.error('error fetching client from pool', err)
    }
    
    client.query('SELECT "Id", "RoleId", "Level" FROM "Person";', function(err, result) {
      done()
      
      if(err) {
        response.end()
        return console.error('error running query', err)
      }
      
      response.render("home", { data: JSON.stringify(result.rows) })
    })
  })  
}) */

app.listen(app.get('port'), function() {
  console.log("Node app is running at localhost:" + app.get('port'))
})

/* const { Client } = require('pg')

const client = new Client({
  user: 'bcfodeyhmhsrcn',
  host: 'ec2-54-197-230-161.compute-1.amazonaws.com',
  database: 'd9lmukb58d9f3g',
  password: '39021d98ad2a8aeaffda294b550bd73f560e423afba332b26e7c925dcf846a21',
  port: 5432,
  ssl: true,
})

client.connect()

client.query('SELECT "Id", "RoleId", "Level" FROM "Person";', (err, res) => {
  if (err) throw err
  for (let row of res.rows) {
    console.log(JSON.stringify(row))
  }
  client.end()
}) */
