const pg = require('pg')
const config = {
  user: 'bcfodeyhmhsrcn',
  host: 'ec2-54-197-230-161.compute-1.amazonaws.com',
  database: 'd9lmukb58d9f3g',
  password: '39021d98ad2a8aeaffda294b550bd73f560e423afba332b26e7c925dcf846a21',
  port: 5432,
  ssl: true,
}
const pool = new pg.Pool(config)

const getHomeView = function(request, response) {
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
      
      return response.render("home", { data: result.rows })
    })
  })  
}

exports.getHomeView = getHomeView
